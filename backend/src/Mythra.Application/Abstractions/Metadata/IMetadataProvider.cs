using Mythra.Domain.Media;

namespace Mythra.Application.Abstractions.Metadata;

public sealed record MetadataSearchResult(
    string ProviderId,
    string Title,
    string? OriginalTitle,
    string? Overview,
    DateOnly? ReleaseDate,
    string? PosterUrl,
    string? BackdropUrl,
    double? Rating,
    IReadOnlyList<string> Genres,
    IReadOnlyDictionary<string, string> ProviderIds,
    bool IsAdult = false);

public interface IMetadataProvider
{
    string Name { get; }
    bool Supports(MediaKind kind);
    Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default);
    Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default);
}

public interface IMetadataProviderRegistry
{
    IMetadataProvider? GetByName(string name);
    IReadOnlyList<IMetadataProvider> ProvidersFor(MediaKind kind);

    /// <summary>Dynamically register a provider (called by the addon host after loading an addon).</summary>
    void Register(IMetadataProvider provider);

    /// <summary>Unregister a provider by name (called when an addon is unloaded).</summary>
    void Unregister(string providerName);
}
