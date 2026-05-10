using Mythra.Addons.Contracts.Models;

namespace Mythra.Addons.Contracts;

/// <summary>
/// Addon that resolves readable links, chapters, and pages for books, manga, and audiobooks.
/// Multiple book source addons can be registered; the host accumulates results from all that
/// support the requested media kind.
/// </summary>
public interface IBookSourceAddon : IAddon
{
    bool Supports(AddonMediaKind kind);

    /// <summary>Lower number = tried first.</summary>
    int Priority { get; }

    /// <summary>
    /// Resolve readable links for a media item. Return empty — never throw — when unavailable.
    /// For manga, each result typically represents a chapter link.
    /// </summary>
    Task<IReadOnlyList<AddonBookResult>> GetLinksAsync(
        AddonBookRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Fetch ordered chapter list for a manga. Return empty when unsupported or unavailable.
    /// </summary>
    Task<IReadOnlyList<AddonMangaChapter>> GetChaptersAsync(
        string mangaId,
        string? language = null,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AddonMangaChapter>>([]);

    /// <summary>
    /// Fetch page image URLs for a chapter. Return null when unsupported or unavailable.
    /// </summary>
    Task<AddonMangaPages?> GetPagesAsync(
        string chapterId,
        CancellationToken ct = default)
        => Task.FromResult<AddonMangaPages?>(null);
}

public sealed record AddonBookRequest(
    Guid          MediaItemId,
    string        Title,
    AddonMediaKind Kind,
    string?       MangaDexId    = null,
    string?       AniListId     = null,
    string?       Author        = null,
    string?       GutenbergId   = null,
    string?       GoogleBooksId = null,
    string?       Isbn          = null);

public sealed record AddonBookResult(
    AddonBookFormat Format,
    string          Url,
    string?         Language = null,
    string?         Title    = null);

public enum AddonBookFormat
{
    Epub      = 1,
    Pdf       = 2,
    PlainText = 3,
    Mp3       = 4,
    WebReader = 5,
}

public sealed record AddonMangaChapter(
    string          ChapterId,
    string?         ChapterNumber,
    string?         Volume,
    string?         Title,
    string          Language,
    int             Pages,
    DateTimeOffset? PublishedAt = null);

/// <summary>Page image URLs for a manga chapter, in reading order.</summary>
public sealed record AddonMangaPages(IReadOnlyList<string> Pages);
