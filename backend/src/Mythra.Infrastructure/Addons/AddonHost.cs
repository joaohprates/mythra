using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;
using Mythra.Application.Abstractions.Addons;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// IHostedService that scans the addons directory at startup, loads each addon
/// into an isolated AssemblyLoadContext, initializes it, and registers it in
/// the appropriate registries (IMetadataProviderRegistry, etc.).
///
/// Unloading an addon disposes its IAddon instance and calls Unload() on the ALC,
/// allowing the GC to collect the addon's assemblies.
/// </summary>
public sealed class AddonHost(
    IOptions<AddonOptions> options,
    ILoggerFactory loggerFactory,
    IMemoryCache cache,
    IHttpClientFactory httpClientFactory,
    IMetadataProviderRegistry metadataRegistry,
    IServiceScopeFactory scopeFactory)
    : IAddonHost, IHostedService, IAsyncDisposable
{
    private readonly ILogger _log = loggerFactory.CreateLogger<AddonHost>();
    private readonly AddonOptions _opts = options.Value;

    // addonId → loaded state
    private readonly ConcurrentDictionary<string, AddonEntry> _entries = new();
    private Timer? _healthTimer;

    // ── IAddonHost ────────────────────────────────────────────────────────────

    public IReadOnlyList<IMetadataAddon>   MetadataAddons    => _entries.Values.Select(e => e.Addon).OfType<IMetadataAddon>().ToList();
    public IReadOnlyList<IStreamSourceAddon> StreamSourceAddons => _entries.Values.Select(e => e.Addon).OfType<IStreamSourceAddon>().ToList();
    public IReadOnlyList<ISubtitleAddon>   SubtitleAddons    => _entries.Values.Select(e => e.Addon).OfType<ISubtitleAddon>().ToList();

    public IAddon? GetById(string addonId) =>
        _entries.TryGetValue(addonId, out var e) ? e.Addon : null;

    public IReadOnlyList<LoadedAddonInfo> GetLoadedAddons() =>
        _entries.Values.Select(e => new LoadedAddonInfo(
            e.Manifest.Id, e.Manifest.Name, e.Manifest.Version,
            e.Manifest.Capabilities, e.Health, e.LoadedAt)).ToList();

    // ── IHostedService ────────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken ct)
    {
        var addonsDir = Path.GetFullPath(_opts.Directory);
        if (!Directory.Exists(addonsDir))
        {
            _log.LogInformation("Addons directory not found at {Path} — no addons loaded.", addonsDir);
            return;
        }

        foreach (var dir in Directory.GetDirectories(addonsDir))
            await TryLoadAddonAsync(dir, ct);

        _healthTimer = new Timer(RunHealthChecks, null,
            _opts.HealthCheckInterval, _opts.HealthCheckInterval);

        _log.LogInformation("Addon host started. {Count} addon(s) loaded.", _entries.Count);
    }

    public async Task StopAsync(CancellationToken ct)
    {
        if (_healthTimer is not null) await _healthTimer.DisposeAsync();
        foreach (var id in _entries.Keys.ToList())
            await UnloadAddonAsync(id, ct);
    }

    // ── Dynamic load / unload ─────────────────────────────────────────────────

    public async Task<bool> TryLoadAddonAsync(string addonDirectory, CancellationToken ct = default)
    {
        var manifestPath = Path.Combine(addonDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            _log.LogWarning("Skipping {Dir}: no manifest.json found.", addonDirectory);
            return false;
        }

        AddonManifest? manifest;
        try
        {
            await using var fs = File.OpenRead(manifestPath);
            manifest = await JsonSerializer.DeserializeAsync<AddonManifest>(fs,
                new JsonSerializerOptions(JsonSerializerDefaults.Web), ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to parse manifest at {Path}.", manifestPath);
            return false;
        }

        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Id))
        {
            _log.LogError("manifest.json at {Path} is invalid (missing id).", manifestPath);
            return false;
        }

        if (_entries.ContainsKey(manifest.Id))
        {
            _log.LogWarning("Addon {Id} is already loaded.", manifest.Id);
            return false;
        }

        var entryDll = Path.Combine(addonDirectory, manifest.EntryPoint.Assembly);
        if (!File.Exists(entryDll))
        {
            _log.LogError("Entry assembly {Dll} not found for addon {Id}.", entryDll, manifest.Id);
            return false;
        }

        // ── Resolve config/secrets from the DB (Addon entity) ─────────────────
        var (configJson, secretsJson) = await LoadAddonStorageAsync(manifest.Id, ct);

        // ── Load assembly in isolated ALC ────────────────────────────────────
        var alc = new AddonAssemblyContext(manifest.Id, entryDll);
        Assembly assembly;
        try { assembly = alc.LoadFromAssemblyPath(entryDll); }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load assembly {Dll} for addon {Id}.", entryDll, manifest.Id);
            alc.Unload();
            return false;
        }

        var addonType = assembly.GetType(manifest.EntryPoint.Type);
        if (addonType is null || !typeof(IAddon).IsAssignableFrom(addonType))
        {
            _log.LogError("Type {Type} not found or does not implement IAddon in addon {Id}.",
                manifest.EntryPoint.Type, manifest.Id);
            alc.Unload();
            return false;
        }

        IAddon addon;
        try { addon = (IAddon)Activator.CreateInstance(addonType)!; }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to instantiate addon {Id}.", manifest.Id);
            alc.Unload();
            return false;
        }

        // ── Build sandboxed context ───────────────────────────────────────────
        var grantedPermissions = manifest.RequiredPermissions
            .Aggregate(AddonPermission.None, (acc, p) => acc | p);

        var context = new AddonSandboxContext(
            addonId:           manifest.Id,
            grantedPermissions: grantedPermissions,
            loggerFactory:     loggerFactory,
            cache:             cache,
            httpClientFactory: httpClientFactory,
            defaultCacheTtl:   _opts.DefaultCacheTtl,
            configJson:        configJson,
            secretsJson:       secretsJson);

        // ── Initialize addon ──────────────────────────────────────────────────
        try { await addon.InitializeAsync(context, ct); }
        catch (Exception ex)
        {
            _log.LogError(ex, "Addon {Id} threw during InitializeAsync — not loading.", manifest.Id);
            await addon.DisposeAsync();
            alc.Unload();
            return false;
        }

        var entry = new AddonEntry(manifest, addon, alc, AddonHealthStatus.Healthy, DateTimeOffset.UtcNow);
        _entries[manifest.Id] = entry;

        // ── Register in downstream registries ────────────────────────────────
        RegisterInRegistries(manifest.Id, addon);

        _log.LogInformation("Loaded addon [{Id}] {Name} v{Version}.",
            manifest.Id, manifest.Name, manifest.Version);
        return true;
    }

    public async Task UnloadAddonAsync(string addonId, CancellationToken ct = default)
    {
        if (!_entries.TryRemove(addonId, out var entry)) return;

        // Deregister from registries first
        if (entry.Addon is IMetadataAddon)
            metadataRegistry.Unregister(addonId);

        try { await entry.Addon.DisposeAsync(); }
        catch (Exception ex) { _log.LogWarning(ex, "Error disposing addon {Id}.", addonId); }

        entry.AssemblyContext.Unload();
        _log.LogInformation("Unloaded addon {Id}.", addonId);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void RegisterInRegistries(string addonId, IAddon addon)
    {
        if (addon is IMetadataAddon metaAddon)
        {
            var bridge = new AddonMetadataBridge(metaAddon, loggerFactory.CreateLogger($"AddonBridge.{addonId}"));
            metadataRegistry.Register(bridge);
        }
        // TODO: register IStreamSourceAddon and ISubtitleAddon in their own registries
    }

    private async Task<(string configJson, string? secretsJson)> LoadAddonStorageAsync(
        string addonId, CancellationToken ct)
    {
        // Look up the persisted Addon entity to get user-configured config/secrets.
        // Uses a short-lived scope since IAddonRepository is Scoped (EF Core).
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetService<IAddonRepository>();
            if (repo is null) return ("{}", null);

            var all = await repo.ListByUserAsync(Guid.Empty, ct); // Guid.Empty = system-level
            var matching = all.FirstOrDefault(a => a.ProviderType == addonId);
            return matching is null
                ? ("{}", null)
                : (matching.ProviderConfigJson, matching.SecretsJson);
        }
        catch
        {
            return ("{}", null);
        }
    }

    private void RunHealthChecks(object? _)
    {
        foreach (var (id, entry) in _entries)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var status = await entry.Addon.HealthCheckAsync();
                    if (entry.Health != status)
                    {
                        _entries[id] = entry with { Health = status };
                        _log.LogInformation("Addon {Id} health changed to {Status}.", id, status);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Health check failed for addon {Id}.", id);
                    _entries[id] = entry with { Health = AddonHealthStatus.Unhealthy };
                }
            });
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_healthTimer is not null) await _healthTimer.DisposeAsync();
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private sealed record AddonEntry(
        AddonManifest Manifest,
        IAddon Addon,
        AddonAssemblyContext AssemblyContext,
        AddonHealthStatus Health,
        DateTimeOffset LoadedAt)
    {
        public AddonHealthStatus Health { get; init; } = Health;
    }
}
