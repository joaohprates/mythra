using System.Collections.Concurrent;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

/// <summary>
/// Thread-safe metadata provider registry.
/// Built-in providers (TMDb, AniList, etc.) are registered at DI configuration time.
/// Addon-backed providers are registered/unregistered dynamically by the AddonHost.
/// </summary>
public sealed class MetadataProviderRegistry : IMetadataProviderRegistry
{
    // ConcurrentDictionary keyed by provider name (case-insensitive) for O(1) lookup.
    private readonly ConcurrentDictionary<string, IMetadataProvider> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    public MetadataProviderRegistry(IEnumerable<IMetadataProvider> builtInProviders)
    {
        foreach (var p in builtInProviders)
            _providers[p.Name] = p;
    }

    public IMetadataProvider? GetByName(string name) =>
        _providers.TryGetValue(name, out var p) ? p : null;

    public IReadOnlyList<IMetadataProvider> ProvidersFor(MediaKind kind) =>
        _providers.Values.Where(p => p.Supports(kind)).ToList();

    public void Register(IMetadataProvider provider) =>
        _providers[provider.Name] = provider;

    public void Unregister(string providerName) =>
        _providers.TryRemove(providerName, out _);
}
