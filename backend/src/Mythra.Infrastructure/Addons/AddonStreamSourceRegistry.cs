using System.Collections.Concurrent;
using Mythra.Application.Abstractions.Providers;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Thread-safe in-memory registry of addon-provided external video providers.
/// Keyed by addon id so unloading an addon removes its bridge cleanly.
/// </summary>
public sealed class AddonStreamSourceRegistry : IAddonStreamSourceRegistry
{
    private readonly ConcurrentDictionary<string, IExternalVideoProvider> _entries = new();

    public void Register(string addonId, IExternalVideoProvider provider)
        => _entries[addonId] = provider;

    public void Unregister(string addonId)
        => _entries.TryRemove(addonId, out _);

    public IReadOnlyList<IExternalVideoProvider> GetAll()
        => _entries.Values.ToList();
}

/// <summary>
/// Thread-safe in-memory registry of addon-provided external book/manga providers.
/// </summary>
public sealed class AddonBookSourceRegistry : IAddonBookSourceRegistry
{
    private readonly ConcurrentDictionary<string, IExternalBookProvider> _entries = new();

    public void Register(string addonId, IExternalBookProvider provider)
        => _entries[addonId] = provider;

    public void Unregister(string addonId)
        => _entries.TryRemove(addonId, out _);

    public IReadOnlyList<IExternalBookProvider> GetAll()
        => _entries.Values.ToList();
}
