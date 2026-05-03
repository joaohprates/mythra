namespace Mythra.Addons.Contracts.Models;

/// <summary>
/// Metadata result returned by IMetadataAddon.
/// ExternalIds carries provider-specific IDs (e.g. "imdb" → "tt0133093",
/// "tmdb" → "movie:603") so the host can store and cross-reference them.
/// </summary>
public sealed record AddonMetadataResult(
    /// <summary>This provider's canonical ID for the item.</summary>
    string ProviderId,
    string Title,
    string? OriginalTitle,
    string? Overview,
    DateOnly? ReleaseDate,
    string? PosterUrl,
    string? BackdropUrl,
    double? Rating,
    int? VoteCount,
    IReadOnlyList<string> Genres,
    /// <summary>Cross-provider IDs keyed by provider name (lowercase).</summary>
    IReadOnlyDictionary<string, string> ExternalIds,
    bool IsAdult = false);
