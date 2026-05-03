using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mythra.Addons.Contracts.Models;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Concrete IAddonContext passed to each addon.
/// All methods enforce the addon's declared permissions before acting.
/// Cache keys are namespaced per addon to prevent cross-addon leakage.
/// </summary>
internal sealed class AddonSandboxContext : IAddonContext
{
    private readonly AddonPermission _grantedPermissions;
    private readonly IMemoryCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeSpan _defaultCacheTtl;
    private readonly string _cachePrefix;
    private readonly Dictionary<string, string> _config;
    private readonly Dictionary<string, string> _secrets;

    public ILogger Logger { get; }

    public AddonSandboxContext(
        string addonId,
        AddonPermission grantedPermissions,
        ILoggerFactory loggerFactory,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory,
        TimeSpan defaultCacheTtl,
        string configJson,
        string? secretsJson)
    {
        _grantedPermissions = grantedPermissions;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _defaultCacheTtl = defaultCacheTtl;
        _cachePrefix = $"addon:{addonId}:";

        Logger = loggerFactory.CreateLogger($"Addon.{addonId}");

        _config = ParseJson(configJson);
        _secrets = ParseJson(secretsJson ?? "{}");
    }

    // ── HTTP ─────────────────────────────────────────────────────────────────

    public HttpClient GetHttpClient(string? baseUrl = null)
    {
        RequirePermission(AddonPermission.Network);
        var client = _httpClientFactory.CreateClient("AddonHttpClient");
        if (!string.IsNullOrWhiteSpace(baseUrl))
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        return client;
    }

    // ── Cache ─────────────────────────────────────────────────────────────────

    public Task<T?> GetCachedAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        RequirePermission(AddonPermission.Cache);
        var value = _cache.TryGetValue<T>(_cachePrefix + key, out var cached) ? cached : null;
        return Task.FromResult(value);
    }

    public Task SetCachedAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        RequirePermission(AddonPermission.Cache);
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? _defaultCacheTtl,
        };
        _cache.Set(_cachePrefix + key, value, options);
        return Task.CompletedTask;
    }

    public Task EvictCachedAsync(string key, CancellationToken ct = default)
    {
        RequirePermission(AddonPermission.Cache);
        _cache.Remove(_cachePrefix + key);
        return Task.CompletedTask;
    }

    // ── Config / Secrets ──────────────────────────────────────────────────────

    public string? GetConfig(string key)
    {
        RequirePermission(AddonPermission.ReadConfig);
        return _config.TryGetValue(key, out var v) ? v : null;
    }

    public string? GetSecret(string key)
    {
        RequirePermission(AddonPermission.ReadSecrets);
        return _secrets.TryGetValue(key, out var v) ? v : null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RequirePermission(AddonPermission required)
    {
        if (!_grantedPermissions.HasFlag(required))
            throw new AddonPermissionException(required);
    }

    private static Dictionary<string, string> ParseJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? [];
        }
        catch
        {
            return [];
        }
    }
}

public sealed class AddonPermissionException(AddonPermission required)
    : InvalidOperationException($"Addon requires permission '{required}' which was not granted.");
