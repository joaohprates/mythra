using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Services.Notifications;
using Mythra.Domain.Common;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Audio;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;
using Mythra.Domain.Notifications;

namespace Mythra.Application.Services.Discover;

public sealed class DiscoverService(
    IMetadataProviderRegistry metadataRegistry,
    ILibraryRepository libraryRepo,
    IMediaItemRepository mediaItemRepo,
    IVideoRepository videoRepo,
    IBookRepository bookRepo,
    IMangaRepository mangaRepo,
    IAudioRepository audioRepo,
    INotificationService notificationService,
    IUnitOfWork uow,
    ILogger<DiscoverService> log) : IDiscoverService
{
    public async Task<Result<DiscoverResultDto>> SearchAsync(
        string query, MediaKind kind, int skip, int take, CancellationToken ct = default)
    {
        var providers = metadataRegistry.ProvidersFor(kind);
        if (providers.Count == 0)
            return Error.Validation($"No metadata provider supports kind '{kind}'.");

        var provider = providers[0];
        var results = await provider.SearchAsync(query, kind, null, ct);

        var items = new List<DiscoverItemDto>();
        foreach (var r in results.Skip(skip).Take(take))
        {
            // Check if already imported
            Guid? existingId = null;
            bool alreadyImported = false;
            if (r.ProviderIds.TryGetValue("tmdb", out var tmdbId))
            {
                var existing = await mediaItemRepo.GetByProviderIdAsync("tmdb", tmdbId, ct);
                if (existing is not null) { alreadyImported = true; existingId = existing.Id; }
            }
            if (!alreadyImported && r.ProviderIds.TryGetValue("imdb", out var imdbId))
            {
                var existing = await mediaItemRepo.GetByProviderIdAsync("imdb", imdbId, ct);
                if (existing is not null) { alreadyImported = true; existingId = existing.Id; }
            }

            items.Add(new DiscoverItemDto(
                ExternalId:     r.ProviderId,
                ProviderKind:   provider.Name,
                Title:          r.Title,
                OriginalTitle:  r.OriginalTitle,
                Year:           r.ReleaseDate?.Year,
                Rating:         r.Rating,
                Overview:       r.Overview,
                PosterPath:     r.PosterUrl,
                BackdropPath:   r.BackdropUrl,
                Genres:         r.Genres,
                AlreadyImported: alreadyImported,
                ExistingItemId: existingId?.ToString()));
        }

        return new DiscoverResultDto(items, results.Count, skip, take);
    }

    public async Task<Result<ImportResultDto>> ImportAsync(ImportExternalRequest req, CancellationToken ct = default)
    {
        // 1. Resolve metadata
        var provider = metadataRegistry.GetByName(req.ProviderKind)
            ?? metadataRegistry.ProvidersFor(req.MediaKind).FirstOrDefault();

        if (provider is null)
            return Error.Validation($"Provider '{req.ProviderKind}' not found or does not support {req.MediaKind}.");

        var metadata = await provider.GetByIdAsync(req.ExternalId, req.MediaKind, ct);
        if (metadata is null)
            return Error.NotFound("ExternalItem", req.ExternalId);

        // 2. Check for duplicates
        if (metadata.ProviderIds.TryGetValue("tmdb", out var tmdb))
        {
            var dup = await mediaItemRepo.GetByProviderIdAsync("tmdb", tmdb, ct);
            if (dup is not null)
                return Error.Conflict($"Item already imported. Existing ID: {dup.Id}");
        }

        // 3. Resolve or create target library
        var libraryId = req.TargetLibraryId ?? await EnsureExternalLibraryAsync(req.MediaKind, ct);

        // 4. Create the MediaItem (no FilePath — external only)
        if (req.MediaKind is not (MediaKind.Video or MediaKind.Book or MediaKind.Manga or MediaKind.Audio))
            return Error.Validation($"Unsupported media kind: {req.MediaKind}");

        MediaItem created = req.MediaKind switch
        {
            MediaKind.Video => CreateVideoItem(libraryId, metadata),
            MediaKind.Book  => CreateBookItem(libraryId, metadata),
            MediaKind.Manga => CreateMangaItem(libraryId, metadata),
            _               => CreateAudioItem(libraryId, metadata),
        };

        switch (created)
        {
            case VideoItem v: await videoRepo.AddAsync(v, ct); break;
            case BookItem  b: await bookRepo.AddAsync(b, ct);  break;
            case MangaItem m: await mangaRepo.AddAsync(m, ct); break;
            case AudioItem a: await audioRepo.AddAsync(a, ct); break;
        }

        await uow.SaveChangesAsync(ct);
        log.LogInformation("Imported external item '{Title}' ({Kind}) via {Provider}", created.Title, req.MediaKind, req.ProviderKind);

        // 5. Notification
        await notificationService.CreateAsync(Notification.Create(
            NotificationKind.ImportCompleted,
            title: $"{created.Title} imported",
            body:  $"Added via {req.ProviderKind}",
            actionUrl: $"/item/{created.Id}",
            imageUrl:  created.PosterPath), ct);

        var watchUrl = req.MediaKind switch
        {
            MediaKind.Video => $"/watch/{created.Id}",
            MediaKind.Book  => $"/read/{created.Id}",
            MediaKind.Manga => $"/read/{created.Id}",
            MediaKind.Audio => $"/listen/{created.Id}",
            _ => $"/item/{created.Id}",
        };

        return new ImportResultDto(
            created.Id, created.Title, req.MediaKind.ToString(),
            HasFile: false, FileStatus: "ExternalOnly",
            created.PosterPath, libraryId, watchUrl);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid> EnsureExternalLibraryAsync(MediaKind kind, CancellationToken ct)
    {
        // Prefer the system library (/media) as the default target
        var systemLib = await libraryRepo.GetSystemLibraryAsync(ct);
        if (systemLib is not null) return systemLib.Id;

        // Fallback: use or create a kind-specific external library
        var libName = $"External {kind}s";
        var existing = await libraryRepo.GetByNameAsync(libName, ct);
        if (existing is not null) return existing.Id;

        var libKind = kind switch
        {
            MediaKind.Video => LibraryKind.Video,
            MediaKind.Book  => LibraryKind.Book,
            MediaKind.Manga => LibraryKind.Manga,
            MediaKind.Audio => LibraryKind.Audiobook,
            _ => LibraryKind.Video,
        };

        var lib = new Library(libName, libKind)
        {
            Description = "Content imported via Discover — streamed via external providers.",
            IsEnabled = true,
        };
        await libraryRepo.AddAsync(lib, ct);
        await uow.SaveChangesAsync(ct);
        return lib.Id;
    }

    private static VideoItem CreateVideoItem(Guid libId, MetadataSearchResult m)
    {
        var item = new VideoItem
        {
            LibraryId      = libId,
            Title          = m.Title,
            OriginalTitle  = m.OriginalTitle,
            Overview       = m.Overview,
            ReleaseDate    = m.ReleaseDate,
            Rating         = m.Rating,
            PosterPath     = m.PosterUrl,
            BackdropPath   = m.BackdropUrl,
            VideoKind      = VideoKind.Movie,
        };
        ApplyProviderIds(item, m.ProviderIds);
        foreach (var g in m.Genres) item.Genres.Add(new Domain.Media.Genre(g));
        return item;
    }

    private static BookItem CreateBookItem(Guid libId, MetadataSearchResult m)
    {
        var item = new BookItem
        {
            LibraryId  = libId,
            Title      = m.Title,
            Overview   = m.Overview,
            ReleaseDate = m.ReleaseDate,
            Rating     = m.Rating,
            PosterPath = m.PosterUrl,
        };
        ApplyProviderIds(item, m.ProviderIds);
        return item;
    }

    private static MangaItem CreateMangaItem(Guid libId, MetadataSearchResult m)
    {
        var item = new MangaItem
        {
            LibraryId  = libId,
            Title      = m.Title,
            Overview   = m.Overview,
            ReleaseDate = m.ReleaseDate,
            Rating     = m.Rating,
            PosterPath = m.PosterUrl,
        };
        ApplyProviderIds(item, m.ProviderIds);
        return item;
    }

    private static AudioItem CreateAudioItem(Guid libId, MetadataSearchResult m)
    {
        var item = new AudioItem
        {
            LibraryId  = libId,
            Title      = m.Title,
            Overview   = m.Overview,
            ReleaseDate = m.ReleaseDate,
            Rating     = m.Rating,
            PosterPath = m.PosterUrl,
            AudioKind  = Domain.Media.Audio.AudioKind.Audiobook,
        };
        ApplyProviderIds(item, m.ProviderIds);
        return item;
    }

    private static void ApplyProviderIds(MediaItem item, IReadOnlyDictionary<string, string> ids)
    {
        if (ids.TryGetValue("tmdb",       out var tmdb))  item.ProviderTmdbId       = tmdb;
        if (ids.TryGetValue("imdb",       out var imdb))  item.ProviderImdbId       = imdb;
        if (ids.TryGetValue("anilist",    out var al))    item.ProviderAnilistId    = al;
        if (ids.TryGetValue("mal",        out var mal))   item.ProviderMalId        = mal;
        if (ids.TryGetValue("google",     out var gb))    item.ProviderGoogleBooksId= gb;
        if (ids.TryGetValue("gutenberg",  out var gut))   item.ProviderGutenbergId  = gut;
        if (ids.TryGetValue("librivox",   out var lv))    item.ProviderLibriVoxId   = lv;
        if (ids.TryGetValue("mangadex",   out var md))    item.ProviderMangaDexId   = md;
    }
}
