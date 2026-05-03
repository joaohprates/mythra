using Mythra.Addons.Contracts.Models;

namespace Mythra.Addons.Contracts;

/// <summary>
/// Addon that provides media metadata (search results, detailed info, cross-ID resolution).
/// A single addon can support multiple AddonMediaKinds.
/// </summary>
public interface IMetadataAddon : IAddon
{
    bool Supports(AddonMediaKind kind);

    /// <summary>
    /// Full-text search. Return an empty list — never throw — when nothing is found.
    /// Results should be ordered by relevance descending.
    /// </summary>
    Task<IReadOnlyList<AddonMetadataResult>> SearchAsync(
        string query,
        AddonMediaKind kind,
        int? year = null,
        CancellationToken ct = default);

    /// <summary>
    /// Fetch a single item by this provider's own ID format (e.g. "tt0133093" for OMDb,
    /// "movie:603" for TMDb). Return null — never throw — when not found.
    /// </summary>
    Task<AddonMetadataResult?> GetByProviderIdAsync(
        string providerId,
        AddonMediaKind kind,
        CancellationToken ct = default);

    /// <summary>
    /// Given an IMDb ID, return this provider's own ID for the same item.
    /// Allows the host to cross-reference providers.
    /// Return null if unsupported or not found.
    /// </summary>
    Task<string?> ResolveImdbIdAsync(
        string imdbId,
        AddonMediaKind kind,
        CancellationToken ct = default);
}
