using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Abstractions.Persistence;

public sealed record MediaQuery(
    Guid? LibraryId = null,
    MediaKind? Kind = null,
    string? Search = null,
    string? Genre = null,
    int? Year = null,
    int Skip = 0,
    int Take = 50,
    string OrderBy = "title",
    IReadOnlyList<Guid>? Ids = null,
    bool? IsAdult = null);

public interface IMediaItemRepository : IRepository<MediaItem>
{
    Task<MediaItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<MediaItem>> SearchAsync(MediaQuery query, CancellationToken ct = default);
    Task<int> CountAsync(MediaQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<MediaItem>> RecentlyAddedAsync(Guid? libraryId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<MediaItem>> ByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    /// <summary>Finds the first item that has a matching provider ID (e.g. "tmdb" → "155").</summary>
    Task<MediaItem?> GetByProviderIdAsync(string providerKind, string providerId, CancellationToken ct = default);
    /// <summary>Resolves an external ID string (e.g. "tt1234567", "movie:123", "anilist:456") to a MediaItem.</summary>
    Task<MediaItem?> GetByExternalIdAsync(string externalId, CancellationToken ct = default);
}

public interface IVideoRepository : IRepository<VideoItem>
{
    Task<VideoItem?> GetByPathAsync(string path, CancellationToken ct = default);
    Task<VideoItem?> GetByIdWithStreamsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<VideoItem>> ListEpisodesAsync(Guid seriesId, CancellationToken ct = default);
}

public interface IMangaRepository : IRepository<MangaItem>
{
    Task<MangaItem?> GetByIdWithChaptersAsync(Guid id, CancellationToken ct = default);
    Task<MangaItem?> GetByRootPathAsync(string rootPath, CancellationToken ct = default);
    Task<MangaChapter?> GetChapterAsync(Guid chapterId, CancellationToken ct = default);
}

public interface IBookRepository : IRepository<BookItem>
{
    Task<BookItem?> GetByIdWithChaptersAsync(Guid id, CancellationToken ct = default);
    Task<BookItem?> GetByPathAsync(string path, CancellationToken ct = default);
}

public interface IGenreRepository : IRepository<Genre>
{
    Task<Genre?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Genre>> ListAsync(MediaKind? kind, CancellationToken ct = default);
    /// <summary>
    /// Returns existing genres for the given names, creating only those that don't exist yet.
    /// Prevents unique-constraint violations when the same genre is shared across multiple items.
    /// </summary>
    Task<IReadOnlyList<Genre>> GetOrCreateManyAsync(IEnumerable<string> names, CancellationToken ct = default);
}

public interface ITagRepository : IRepository<Tag>
{
    Task<Tag?> GetBySlugAsync(string slug, CancellationToken ct = default);
}
