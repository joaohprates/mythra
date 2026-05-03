using Mythra.Domain.Media;

namespace Mythra.Application.Abstractions.Metadata;

/// <summary>
/// Catalog browsing capability — for providers that expose curated lists
/// (popular, trending, top-rated) in addition to free-text search.
/// </summary>
public interface ICatalogProvider
{
    string Name { get; }
    bool SupportsCatalog(MediaKind kind, string catalogType);

    /// <param name="catalogType">e.g. "movie" / "series" / "anime".</param>
    /// <param name="category">e.g. "popular" / "top" / "trending" / "year".</param>
    Task<IReadOnlyList<MetadataSearchResult>> GetCatalogAsync(
        MediaKind kind,
        string catalogType,
        string category,
        int skip,
        int take,
        CancellationToken ct = default);
}
