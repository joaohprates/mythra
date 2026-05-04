using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Services.Notifications;
using Mythra.Domain.Common;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;
using Mythra.Domain.Notifications;

namespace Mythra.Application.Services.Discover;

public sealed class DiscoverService(
    IMetadataProviderRegistry metadataRegistry,
    IEnumerable<ICatalogProvider> catalogProviders,
    ILibraryRepository libraryRepo,
    IMediaItemRepository mediaItemRepo,
    IVideoRepository videoRepo,
    IBookRepository bookRepo,
    IMangaRepository mangaRepo,
    INotificationService notificationService,
    IUnitOfWork uow,
    IMemoryCache cache,
    ILogger<DiscoverService> log) : IDiscoverService
{
    private readonly List<ICatalogProvider> _catalogs = catalogProviders.ToList();

    public async Task<Result<DiscoverResultDto>> SearchAsync(DiscoverQuery query, CancellationToken ct = default)
    {
        var hasQuery = !string.IsNullOrWhiteSpace(query.Query);
        var skipCache = hasQuery && (query.Query?.Length ?? 0) > 100;
        var cacheKey = BuildCacheKey(query);

        if (!skipCache && cache.TryGetValue<DiscoverResultDto>(cacheKey, out var cached) && cached is not null)
            return cached;

        IReadOnlyList<MetadataSearchResult> results;
        string usedProvider;

        try
        {
            (results, usedProvider) = hasQuery
                ? await FreeTextSearchAsync(query, ct)
                : await BrowseCatalogAsync(query, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Discover search failed (kind={Kind}, type={Type}, q='{Query}')",
                query.Kind, query.Type, query.Query);
            return new DiscoverResultDto([], 0, query.Skip, query.Take);
        }

        DiscoverResultDto dto;
        if (results.Count == 0)
        {
            dto = new DiscoverResultDto([], query.Skip, query.Skip, query.Take);
        }
        else
        {
            var items = new List<DiscoverItemDto>(results.Count);
            foreach (var r in results)
            {
                Guid? existingId = null;
                var alreadyImported = false;
                foreach (var (key, value) in r.ProviderIds)
                {
                    var existing = await mediaItemRepo.GetByProviderIdAsync(key, value, ct);
                    if (existing is not null) { alreadyImported = true; existingId = existing.Id; break; }
                }

                items.Add(new DiscoverItemDto(
                    ExternalId:      r.ProviderId,
                    ProviderKind:    usedProvider,
                    Title:           r.Title,
                    OriginalTitle:   r.OriginalTitle,
                    Year:            r.ReleaseDate?.Year,
                    Rating:          r.Rating,
                    Overview:        r.Overview,
                    PosterPath:      r.PosterUrl,
                    BackdropPath:    r.BackdropUrl,
                    Genres:          r.Genres,
                    AlreadyImported: alreadyImported,
                    ExistingItemId:  existingId?.ToString(),
                    IsAdult:         r.IsAdult));
            }

            // We don't know the true total upstream — return a generous estimate so the UI can paginate.
            var total = items.Count < query.Take ? query.Skip + items.Count : query.Skip + query.Take + 1;
            dto = new DiscoverResultDto(items, total, query.Skip, query.Take);
        }

        if (!skipCache)
        {
            var ttl = hasQuery ? TimeSpan.FromSeconds(60) : TimeSpan.FromMinutes(5);
            cache.Set(cacheKey, dto, ttl);
        }

        return dto;
    }

    private static string BuildCacheKey(DiscoverQuery q) =>
        $"discover:{q.Type}:{(int)q.Kind}:{q.Category}:{(q.Query ?? "").ToLowerInvariant()}:{q.Skip}:{q.Take}:{q.Provider ?? ""}";

    private async Task<(IReadOnlyList<MetadataSearchResult> Results, string Provider)> BrowseCatalogAsync(
        DiscoverQuery q, CancellationToken ct)
    {
        var providers = _catalogs.Where(c => c.SupportsCatalog(q.Kind, q.Type)).ToList();
        if (q.Provider is not null)
            providers = providers.Where(p => p.Name.Equals(q.Provider, StringComparison.OrdinalIgnoreCase)).ToList();

        if (providers.Count > 0)
        {
            // Fan out in parallel; preserve provider priority order when picking the first non-empty result.
            var tasks = providers
                .Select(p => SafeCatalogCallAsync(p, q, ct))
                .ToArray();
            var allResults = await Task.WhenAll(tasks);
            for (var i = 0; i < providers.Count; i++)
            {
                var items = allResults[i];
                if (items.Count > 0)
                    return (items, providers[i].Name);
            }
        }

        // Fallback to any matching IMetadataProvider's empty-query search (rare path).
        var fallback = metadataRegistry.ProvidersFor(q.Kind);
        var fallbackTasks = fallback
            .Select(p => SafeMetaSearchAsync(p, "", q.Kind, ct))
            .ToArray();
        var fallbackResults = await Task.WhenAll(fallbackTasks);
        for (var i = 0; i < fallback.Count; i++)
        {
            var r = fallbackResults[i];
            if (r.Count > 0) return (r.Skip(q.Skip).Take(q.Take).ToList(), fallback[i].Name);
        }

        return ([], string.Empty);
    }

    private async Task<IReadOnlyList<MetadataSearchResult>> SafeCatalogCallAsync(
        ICatalogProvider p, DiscoverQuery q, CancellationToken ct)
    {
        try
        {
            return await p.GetCatalogAsync(q.Kind, q.Type, q.Category, q.Skip, q.Take, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Catalog provider {Provider} failed.", p.Name);
            return [];
        }
    }

    private async Task<IReadOnlyList<MetadataSearchResult>> SafeMetaSearchAsync(
        IMetadataProvider p, string query, MediaKind kind, CancellationToken ct)
    {
        try
        {
            return await p.SearchAsync(query, kind, null, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Search provider {Provider} failed.", p.Name);
            return [];
        }
    }

    private async Task<(IReadOnlyList<MetadataSearchResult> Results, string Provider)> FreeTextSearchAsync(
        DiscoverQuery q, CancellationToken ct)
    {
        var allProviders = metadataRegistry.ProvidersFor(q.Kind);
        var candidates = q.Provider is not null
            ? allProviders.Where(p => p.Name.Equals(q.Provider, StringComparison.OrdinalIgnoreCase)).ToList()
            : (IReadOnlyList<IMetadataProvider>)allProviders;

        if (candidates.Count == 0) return ([], string.Empty);

        var tasks = candidates
            .Select(p => SafeMetaSearchAsync(p, q.Query!, q.Kind, ct))
            .ToArray();
        var allResults = await Task.WhenAll(tasks);

        for (var i = 0; i < candidates.Count; i++)
        {
            var results = allResults[i];
            if (results.Count > 0)
                return (results.Skip(q.Skip).Take(q.Take).ToList(), candidates[i].Name);
        }

        return ([], string.Empty);
    }

    public async Task<Result<ImportResultDto>> ImportAsync(ImportExternalRequest req, CancellationToken ct = default)
    {
        log.LogInformation(
            "Discover import requested: provider={Provider}, externalId={ExternalId}, kind={Kind}",
            req.ProviderKind, req.ExternalId, req.MediaKind);

        try
        {
            return await ImportCoreAsync(req, ct);
        }
        catch (Exception ex)
        {
            log.LogError(ex,
                "Discover import failed for {Provider}:{ExternalId} ({Kind})",
                req.ProviderKind, req.ExternalId, req.MediaKind);
            return Error.Internal($"Import failed: {ex.Message}");
        }
    }

    private async Task<Result<ImportResultDto>> ImportCoreAsync(ImportExternalRequest req, CancellationToken ct)
    {
        // 1. Resolve metadata — try the named provider first, then any provider that
        //    supports this media kind so the import survives a misnamed ProviderKind.
        var primary  = metadataRegistry.GetByName(req.ProviderKind);
        var fallback = metadataRegistry.ProvidersFor(req.MediaKind);
        var provider = primary ?? fallback.FirstOrDefault();
        var triedFallback = primary is null && provider is not null;

        if (provider is null)
            return Error.Validation($"Provider '{req.ProviderKind}' not found or does not support {req.MediaKind}.");

        var metadata = await provider.GetByIdAsync(req.ExternalId, req.MediaKind, ct);
        if (metadata is null)
        {
            var triedNames = triedFallback
                ? $"{req.ProviderKind} (missing) → fallback {provider.Name}"
                : provider.Name;
            log.LogWarning(
                "Discover import not found: providers tried = {Tried} for externalId {ExternalId} ({Kind})",
                triedNames, req.ExternalId, req.MediaKind);
            return Error.NotFound("ExternalItem", $"{req.ProviderKind}:{req.ExternalId} (tried {triedNames})");
        }

        // 2. Check for duplicates across known provider ids
        foreach (var (key, value) in metadata.ProviderIds)
        {
            var dup = await mediaItemRepo.GetByProviderIdAsync(key, value, ct);
            if (dup is not null)
                return Error.Conflict($"Item already imported. Existing ID: {dup.Id}");
        }

        // 3. Resolve or create target library
        var libraryId = req.TargetLibraryId ?? await EnsureExternalLibraryAsync(req.MediaKind, ct);

        // 4. Create the MediaItem (no FilePath — external only)
        if (req.MediaKind is not (MediaKind.Video or MediaKind.Book or MediaKind.Manga))
            return Error.Validation($"Unsupported media kind: {req.MediaKind}");

        MediaItem created = req.MediaKind switch
        {
            MediaKind.Video => CreateVideoItem(libraryId, metadata),
            MediaKind.Book  => CreateBookItem(libraryId, metadata),
            MediaKind.Manga => CreateMangaItem(libraryId, metadata),
            _               => throw new InvalidOperationException("Unsupported media kind"),
        };

        foreach (var g in metadata.Genres)
            if (!created.Genres.Any(existing => existing.Name == g))
                created.Genres.Add(new Domain.Media.Genre(g));

        // Surface the adult flag through the genres list so the IsAdult filter works.
        if (metadata.IsAdult && !created.Genres.Any(g => g.Name is "Adult Content" or "Hentai" or "Ecchi" or "Adult"))
            created.Genres.Add(new Domain.Media.Genre("Adult Content"));

        switch (created)
        {
            case VideoItem v: await videoRepo.AddAsync(v, ct); break;
            case BookItem  b: await bookRepo.AddAsync(b, ct);  break;
            case MangaItem m: await mangaRepo.AddAsync(m, ct); break;
        }

        await uow.SaveChangesAsync(ct);
        log.LogInformation("Imported external item '{Title}' ({Kind}) via {Provider}",
            created.Title, req.MediaKind, req.ProviderKind);

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
        var systemLib = await libraryRepo.GetSystemLibraryAsync(ct);
        if (systemLib is not null) return systemLib.Id;

        var libName = $"External {kind}s";
        var existing = await libraryRepo.GetByNameAsync(libName, ct);
        if (existing is not null) return existing.Id;

        var libKind = kind switch
        {
            MediaKind.Video => LibraryKind.Video,
            MediaKind.Book  => LibraryKind.Book,
            MediaKind.Manga => LibraryKind.Manga,
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

    private static void ApplyProviderIds(MediaItem item, IReadOnlyDictionary<string, string> ids)
    {
        if (ids.TryGetValue("tmdb",       out var tmdb))  item.ProviderTmdbId       = tmdb;
        if (ids.TryGetValue("imdb",       out var imdb))  item.ProviderImdbId       = imdb;
        if (ids.TryGetValue("anilist",    out var al))    item.ProviderAnilistId    = al;
        if (ids.TryGetValue("mal",        out var mal))   item.ProviderMalId        = mal;
        if (ids.TryGetValue("google",     out var gb))    item.ProviderGoogleBooksId= gb;
        if (ids.TryGetValue("openlibrary",out var ol))    item.ProviderGoogleBooksId= ol;
        if (ids.TryGetValue("gutenberg",  out var gut))   item.ProviderGutenbergId  = gut;
        if (ids.TryGetValue("librivox",   out var lv))    item.ProviderLibriVoxId   = lv;
        if (ids.TryGetValue("mangadex",   out var md))    item.ProviderMangaDexId   = md;
    }
}
