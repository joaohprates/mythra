using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Audio;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class MediaItemRepository(MythraDbContext db) : EfRepository<MediaItem>(db), IMediaItemRepository
{
    public Task<MediaItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(m => m.Genres).Include(m => m.Tags).FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IReadOnlyList<MediaItem>> SearchAsync(MediaQuery query, CancellationToken ct = default)
    {
        var q = ApplyQuery(Set.AsNoTracking().Include(m => m.Genres), query);
        q = OrderByClause(q, query.OrderBy);
        return await q.Skip(query.Skip).Take(Math.Clamp(query.Take, 1, 200)).ToListAsync(ct);
    }

    public Task<int> CountAsync(MediaQuery query, CancellationToken ct = default) =>
        ApplyQuery(Set.AsNoTracking(), query).CountAsync(ct);

    public async Task<IReadOnlyList<MediaItem>> RecentlyAddedAsync(Guid? libraryId, int take, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking().AsQueryable();
        if (libraryId.HasValue) q = q.Where(m => m.LibraryId == libraryId);
        return await q.OrderByDescending(m => m.CreatedAt).Take(Math.Clamp(take, 1, 100)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MediaItem>> ByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await Set.AsNoTracking().Include(m => m.Genres).Where(m => ids.Contains(m.Id)).ToListAsync(ct);

    public Task<MediaItem?> GetByProviderIdAsync(string providerKind, string providerId, CancellationToken ct = default) =>
        providerKind.ToLower() switch
        {
            "tmdb"        => Set.FirstOrDefaultAsync(m => m.ProviderTmdbId        == providerId, ct),
            "imdb"        => Set.FirstOrDefaultAsync(m => m.ProviderImdbId        == providerId, ct),
            "anilist"     => Set.FirstOrDefaultAsync(m => m.ProviderAnilistId     == providerId, ct),
            // openlibrary reuses the ProviderGoogleBooksId column (no migration needed)
            "openlibrary" or "google" => Set.FirstOrDefaultAsync(m => m.ProviderGoogleBooksId == providerId, ct),
            // spotify reuses the ProviderMusicbrainzId column (no migration needed)
            "spotify"     or "musicbrainz" => Set.FirstOrDefaultAsync(m => m.ProviderMusicbrainzId == providerId, ct),
            "gutenberg"   => Set.FirstOrDefaultAsync(m => m.ProviderGutenbergId  == providerId, ct),
            "librivox"    => Set.FirstOrDefaultAsync(m => m.ProviderLibriVoxId   == providerId, ct),
            "mangadex"    => Set.FirstOrDefaultAsync(m => m.ProviderMangaDexId   == providerId, ct),
            _             => Task.FromResult<MediaItem?>(null),
        };

    private static readonly string[] AdultGenreNames =
        ["Hentai", "Ecchi", "Adult", "Adult Content", "Pornography", "Eroge"];

    private static IQueryable<MediaItem> ApplyQuery(IQueryable<MediaItem> q, MediaQuery query)
    {
        if (query.LibraryId.HasValue) q = q.Where(m => m.LibraryId == query.LibraryId);
        if (query.Kind.HasValue) q = q.Where(m => EF.Property<int>(m, "MediaKind") == (int)query.Kind);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.ToLower();
            q = q.Where(m => EF.Functions.Like(m.Title.ToLower(), $"%{s}%")
                         || (m.OriginalTitle != null && EF.Functions.Like(m.OriginalTitle.ToLower(), $"%{s}%")));
        }
        if (!string.IsNullOrWhiteSpace(query.Genre))
            q = q.Where(m => m.Genres.Any(g => g.Slug == query.Genre.ToLower()));
        if (query.Year.HasValue)
            q = q.Where(m => m.ReleaseDate.HasValue && m.ReleaseDate.Value.Year == query.Year);
        if (query.IsAdult.HasValue)
        {
            if (query.IsAdult.Value)
                q = q.Where(m => m.Genres.Any(g => AdultGenreNames.Contains(g.Name)));
            else
                q = q.Where(m => !m.Genres.Any(g => AdultGenreNames.Contains(g.Name)));
        }
        return q;
    }

    private static IQueryable<MediaItem> OrderByClause(IQueryable<MediaItem> q, string orderBy) => orderBy switch
    {
        "title" => q.OrderBy(m => m.Title),
        "-title" => q.OrderByDescending(m => m.Title),
        "year" => q.OrderBy(m => m.ReleaseDate),
        "-year" => q.OrderByDescending(m => m.ReleaseDate),
        "rating" => q.OrderByDescending(m => m.Rating),
        "added" or "-added" => q.OrderByDescending(m => m.CreatedAt),
        _ => q.OrderBy(m => m.Title),
    };
}

public sealed class VideoRepository(MythraDbContext db) : EfRepository<VideoItem>(db), IVideoRepository
{
    public Task<VideoItem?> GetByPathAsync(string path, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(v => v.FilePath == path, ct);

    public Task<VideoItem?> GetByIdWithStreamsAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(v => v.Subtitles)
           .Include(v => v.AudioTracks)
           .Include(v => v.ChapterMarkers)
           .Include(v => v.Genres)
           .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<VideoItem>> ListEpisodesAsync(Guid seriesId, CancellationToken ct = default) =>
        await Set.Where(v => v.ParentId == seriesId)
                 .OrderBy(v => v.SeasonNumber)
                 .ThenBy(v => v.EpisodeNumber)
                 .ToListAsync(ct);
}

public sealed class MangaRepository(MythraDbContext db) : EfRepository<MangaItem>(db), IMangaRepository
{
    public Task<MangaItem?> GetByIdWithChaptersAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(m => m.Chapters).Include(m => m.Genres).FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<MangaItem?> GetByRootPathAsync(string rootPath, CancellationToken ct = default) =>
        Set.Include(m => m.Chapters).FirstOrDefaultAsync(m => m.RootPath == rootPath, ct);

    public Task<MangaChapter?> GetChapterAsync(Guid chapterId, CancellationToken ct = default) =>
        Db.MangaChapters.FirstOrDefaultAsync(c => c.Id == chapterId, ct);
}

public sealed class BookRepository(MythraDbContext db) : EfRepository<BookItem>(db), IBookRepository
{
    public Task<BookItem?> GetByIdWithChaptersAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(b => b.Chapters).Include(b => b.Genres).FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<BookItem?> GetByPathAsync(string path, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(b => b.FilePath == path, ct);
}

public sealed class AudioRepository(MythraDbContext db) : EfRepository<AudioItem>(db), IAudioRepository
{
    public Task<AudioItem?> GetByIdWithChaptersAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(a => a.Chapters).FirstOrDefaultAsync(a => a.Id == id, ct);
}

public sealed class GenreRepository(MythraDbContext db) : EfRepository<Genre>(db), IGenreRepository
{
    public Task<Genre?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(g => g.Slug == slug, ct);

    public async Task<IReadOnlyList<Genre>> ListAsync(MediaKind? kind, CancellationToken ct = default)
    {
        var q = Set.AsQueryable();
        if (kind.HasValue) q = q.Where(g => g.Kind == kind || g.Kind == null);
        return await q.OrderBy(g => g.Name).ToListAsync(ct);
    }
}

public sealed class TagRepository(MythraDbContext db) : EfRepository<Tag>(db), ITagRepository
{
    public Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(t => t.Slug == slug, ct);
}
