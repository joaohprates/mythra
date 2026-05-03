using Microsoft.Extensions.Logging;

namespace Mythra.Addons.Contracts.Models;

/// <summary>
/// Sandboxed access to host services.
/// Injected into the addon during InitializeAsync and available for its full lifetime.
///
/// The host enforces the addon's declared permissions on every method:
/// calling CreateHttpClient() without the Network permission throws AddonPermissionException.
/// </summary>
public interface IAddonContext
{
    /// <summary>Logger scoped to this addon.</summary>
    ILogger Logger { get; }

    // ── HTTP ────────────────────────────────────────────────────────────────
    // Requires: AddonPermission.Network

    /// <summary>
    /// Returns an HttpClient pre-configured with the given base URL.
    /// The host may apply rate-limiting and circuit-breaker policies.
    /// Callers must NOT dispose the returned client.
    /// </summary>
    HttpClient GetHttpClient(string? baseUrl = null);

    // ── Cache ───────────────────────────────────────────────────────────────
    // Requires: AddonPermission.Cache

    /// <summary>Retrieves a cached value. Returns null on miss.</summary>
    Task<T?> GetCachedAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Stores a value in cache. Default TTL: 1 hour.</summary>
    Task SetCachedAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;

    /// <summary>Removes a cached entry.</summary>
    Task EvictCachedAsync(string key, CancellationToken ct = default);

    // ── Configuration ───────────────────────────────────────────────────────
    // Requires: AddonPermission.ReadConfig

    /// <summary>Gets a config value from the addon's ProviderConfigJson.</summary>
    string? GetConfig(string key);

    // ── Secrets ─────────────────────────────────────────────────────────────
    // Requires: AddonPermission.ReadSecrets

    /// <summary>Gets a secret from the addon's SecretsJson (never logged or exported).</summary>
    string? GetSecret(string key);
}
