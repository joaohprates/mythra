using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Addons;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Addons;
using Mythra.Infrastructure.Metadata;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Maps a ProviderType string (e.g. "omdb") to a concrete provider instance.
/// New provider types can be added here without touching any other class.
/// </summary>
public sealed class AddonActivator(
    IMetadataProviderRegistry metadataRegistry,
    IMemoryCache cache,
    IHttpClientFactory httpFactory,
    ILoggerFactory loggerFactory) : IAddonActivator
{
    // All known provider type identifiers (case-insensitive).
    private static readonly HashSet<string> KnownTypes =
        new(StringComparer.OrdinalIgnoreCase) { "omdb" };

    public bool CanHandle(string providerType) => KnownTypes.Contains(providerType);

    public void Activate(Addon addon)
    {
        if (!CanHandle(addon.ProviderType)) return;

        switch (addon.ProviderType.ToLowerInvariant())
        {
            case "omdb":
                ActivateOmdb(addon);
                break;
        }
    }

    public void Deactivate(Addon addon)
    {
        if (!CanHandle(addon.ProviderType)) return;

        // Use the provider's canonical name to remove it from the registry.
        metadataRegistry.Unregister(addon.ProviderType.ToLowerInvariant());
    }

    // ── Provider-specific activation ──────────────────────────────────────────

    private void ActivateOmdb(Addon addon)
    {
        var secrets = ParseJson(addon.SecretsJson);
        if (!secrets.TryGetValue("ApiKey", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            loggerFactory.CreateLogger<AddonActivator>()
                .LogWarning("OMDb addon '{Name}' is active but has no ApiKey secret — skipping.", addon.Name);
            return;
        }

        var config   = ParseJson(addon.ProviderConfigJson);
        var searchTtl = TryParseMinutes(config.GetValueOrDefault("SearchCacheTtlMinutes"), 60);
        var detailTtl = TryParseHours(config.GetValueOrDefault("DetailCacheTtlHours"), 24);
        var prefix   = $"addon:omdb:{addon.Id}:";

        var provider = new OmdbMetadataProvider(
            apiKey:      apiKey,
            httpFactory: httpFactory,
            cache:       cache,
            cachePrefix: prefix,
            searchTtl:   searchTtl,
            detailTtl:   detailTtl,
            logger:      loggerFactory.CreateLogger<OmdbMetadataProvider>());

        metadataRegistry.Register(provider);

        loggerFactory.CreateLogger<AddonActivator>()
            .LogInformation("Activated OMDb metadata provider (addon: '{Name}').", addon.Name);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Dictionary<string, string> ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []; }
        catch { return []; }
    }

    private static TimeSpan TryParseMinutes(string? raw, int def) =>
        int.TryParse(raw, out var m) && m > 0 ? TimeSpan.FromMinutes(m) : TimeSpan.FromMinutes(def);

    private static TimeSpan TryParseHours(string? raw, int def) =>
        int.TryParse(raw, out var h) && h > 0 ? TimeSpan.FromHours(h) : TimeSpan.FromHours(def);
}
