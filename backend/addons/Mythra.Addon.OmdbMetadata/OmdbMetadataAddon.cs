using System.Net.Http.Json;
using System.Text;
using Mythra.Addon.OmdbMetadata.Models;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;

namespace Mythra.Addon.OmdbMetadata;

/// <summary>
/// Mythra addon that fetches movie and series metadata from the OMDb API.
///
/// OMDb is powered by IMDb data, so the ProviderId returned by this addon
/// IS the IMDb ID (e.g. "tt0133093"). This means ExternalIds["imdb"] is always
/// populated, allowing the host to cross-reference with other providers.
///
/// Permissions required: Network, Cache, ReadSecrets, ReadConfig.
///
/// Configuration (via the Mythra UI or JSON import):
///   Secrets.ApiKey             — OMDb API key (free at https://www.omdbapi.com/apikey.aspx)
///   Config.SearchCacheTtlMinutes — default 60
///   Config.DetailCacheTtlHours   — default 24
///
/// Rate limits: free tier allows 1 000 requests/day.
/// Caching keeps us well within that limit for a personal media hub.
/// </summary>
public sealed class OmdbMetadataAddon : IMetadataAddon
{
    // ── IAddon identity (matches manifest.json) ───────────────────────────────
    public string Id      => "io.mythra.omdb-metadata";
    public string Name    => "OMDb / IMDb Metadata";
    public string Version => "1.0.0";

    private IAddonContext _ctx = null!;  // set during InitializeAsync
    private HttpClient    _http = null!;
    private string        _apiKey = null!;
    private TimeSpan      _searchCacheTtl;
    private TimeSpan      _detailCacheTtl;

    private const string BaseUrl = "https://www.omdbapi.com/";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public ValueTask InitializeAsync(IAddonContext context, CancellationToken ct = default)
    {
        _ctx = context;
        _apiKey = context.GetSecret("ApiKey")
            ?? throw new InvalidOperationException(
                "OMDb API key is required. Set Secrets.ApiKey in the addon configuration.");

        _http = context.GetHttpClient(BaseUrl);

        // Read optional TTL config, fall back to sensible defaults.
        _searchCacheTtl = TryParseMinutes(context.GetConfig("SearchCacheTtlMinutes"), 60);
        _detailCacheTtl = TryParseHours(context.GetConfig("DetailCacheTtlHours"), 24);

        _ctx.Logger.LogInformation(
            "[OmdbMetadataAddon] Initialized. SearchTTL={Search}, DetailTTL={Detail}",
            _searchCacheTtl, _detailCacheTtl);

        return ValueTask.CompletedTask;
    }

    public async ValueTask<AddonHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        // Ping the API with a known title to verify the key is still valid.
        try
        {
            var url = BuildUrl(("t", "Inception"), ("type", "movie"));
            var response = await _http.GetFromJsonAsync<OmdbDetailResponse>(url, ct);
            return response?.IsSuccess == true
                ? AddonHealthStatus.Healthy
                : AddonHealthStatus.Degraded;
        }
        catch
        {
            return AddonHealthStatus.Unhealthy;
        }
    }

    public ValueTask DisposeAsync()
    {
        // HttpClient is managed by the host's IHttpClientFactory — don't dispose.
        return ValueTask.CompletedTask;
    }

    // ── IMetadataAddon ────────────────────────────────────────────────────────

    // Only Movie and Series are supported by OMDb.
    public bool Supports(AddonMediaKind kind) =>
        kind is AddonMediaKind.Movie or AddonMediaKind.Series;

    /// <summary>
    /// Search by title. OMDb's search endpoint returns a list of matches.
    /// Each result is a "stub" — poster and year only — so we enrich the
    /// top result with a full detail call to return a proper AddonMetadataResult.
    /// </summary>
    public async Task<IReadOnlyList<AddonMetadataResult>> SearchAsync(
        string query,
        AddonMediaKind kind,
        int? year = null,
        CancellationToken ct = default)
    {
        if (!Supports(kind)) return [];
        if (string.IsNullOrWhiteSpace(query)) return [];

        var cacheKey = $"search:{NormalizeKind(kind)}:{query.ToLowerInvariant()}:{year}";
        var cached = await _ctx.GetCachedAsync<List<AddonMetadataResult>>(cacheKey, ct);
        if (cached is not null) return cached;

        // ── OMDb search by title ──────────────────────────────────────────────
        var args = new List<(string, string)>
        {
            ("s", query),
            ("type", NormalizeKind(kind)),
        };
        if (year.HasValue) args.Add(("y", year.Value.ToString()));

        OmdbSearchResponse? searchResp;
        try
        {
            searchResp = await _http.GetFromJsonAsync<OmdbSearchResponse>(BuildUrl(args.ToArray()), ct);
        }
        catch (Exception ex)
        {
            _ctx.Logger.LogWarning(ex, "[OmdbMetadataAddon] Search request failed for '{Query}'.", query);
            return [];
        }

        if (searchResp is null || !searchResp.IsSuccess || searchResp.Search is null)
        {
            _ctx.Logger.LogDebug("[OmdbMetadataAddon] No results for '{Query}': {Error}",
                query, searchResp?.Error ?? "unknown");
            return [];
        }

        // Fetch full details for the first 3 results (to get ratings, genres, etc.).
        // Beyond 3 we return stub results to stay within OMDb's free-tier limits.
        var results = new List<AddonMetadataResult>();
        var enrichLimit = Math.Min(3, searchResp.Search.Count);

        for (int i = 0; i < searchResp.Search.Count; i++)
        {
            var entry = searchResp.Search[i];
            if (string.IsNullOrWhiteSpace(entry.ImdbId)) continue;

            if (i < enrichLimit)
            {
                // Full detail fetch (cached separately per IMDb ID)
                var detail = await FetchDetailByImdbIdAsync(entry.ImdbId, ct);
                if (detail is not null) { results.Add(detail); continue; }
            }

            // Stub result for the rest
            results.Add(MapSearchEntry(entry));
        }

        await _ctx.SetCachedAsync(cacheKey, results, _searchCacheTtl, ct);
        return results;
    }

    /// <summary>
    /// Fetch full details by this provider's ID (= IMDb ID, e.g. "tt0133093").
    /// </summary>
    public async Task<AddonMetadataResult?> GetByProviderIdAsync(
        string providerId,
        AddonMediaKind kind,
        CancellationToken ct = default)
    {
        if (!Supports(kind)) return null;
        if (string.IsNullOrWhiteSpace(providerId)) return null;

        return await FetchDetailByImdbIdAsync(providerId, ct);
    }

    /// <summary>
    /// Since this provider uses IMDb IDs natively, resolving an IMDb ID is trivial:
    /// we just validate that the item exists and return the same ID.
    /// </summary>
    public async Task<string?> ResolveImdbIdAsync(
        string imdbId,
        AddonMediaKind kind,
        CancellationToken ct = default)
    {
        if (!Supports(kind)) return null;
        var result = await FetchDetailByImdbIdAsync(imdbId, ct);
        return result?.ProviderId;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<AddonMetadataResult?> FetchDetailByImdbIdAsync(
        string imdbId, CancellationToken ct)
    {
        var cacheKey = $"detail:{imdbId}";
        var cached = await _ctx.GetCachedAsync<AddonMetadataResult>(cacheKey, ct);
        if (cached is not null) return cached;

        OmdbDetailResponse? detail;
        try
        {
            detail = await _http.GetFromJsonAsync<OmdbDetailResponse>(
                BuildUrl(("i", imdbId), ("plot", "full")), ct);
        }
        catch (Exception ex)
        {
            _ctx.Logger.LogWarning(ex, "[OmdbMetadataAddon] Detail fetch failed for IMDb ID {Id}.", imdbId);
            return null;
        }

        if (detail is null || !detail.IsSuccess)
        {
            _ctx.Logger.LogDebug("[OmdbMetadataAddon] Not found: {Id} — {Error}", imdbId, detail?.Error);
            return null;
        }

        var result = MapDetail(detail);
        await _ctx.SetCachedAsync(cacheKey, result, _detailCacheTtl, ct);
        return result;
    }

    private AddonMetadataResult MapDetail(OmdbDetailResponse d)
    {
        var genres = d.SafeGenre?.Split(',', StringSplitOptions.TrimEntries)
                        .Where(g => !string.IsNullOrEmpty(g)).ToList()
                     ?? [];

        var externalIds = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(d.ImdbId)) externalIds["imdb"] = d.ImdbId!;

        double? rating = null;
        if (double.TryParse(d.SafeRating, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var r))
            rating = r;

        int? voteCount = null;
        if (!string.IsNullOrWhiteSpace(d.SafeVotes))
        {
            var clean = d.SafeVotes.Replace(",", "").Trim();
            if (int.TryParse(clean, out var v)) voteCount = v;
        }

        DateOnly? releaseDate = null;
        if (!string.IsNullOrWhiteSpace(d.SafeReleased)
            && DateOnly.TryParse(d.SafeReleased, out var dt))
            releaseDate = dt;
        else if (!string.IsNullOrWhiteSpace(d.Year)
                 && int.TryParse(d.Year.AsSpan(0, Math.Min(4, d.Year.Length)), out var yr))
            releaseDate = new DateOnly(yr, 1, 1);

        return new AddonMetadataResult(
            ProviderId:    d.ImdbId ?? string.Empty,
            Title:         d.SafeTitle ?? "Untitled",
            OriginalTitle: null,  // OMDb doesn't separate original/localized titles
            Overview:      d.SafePlot,
            ReleaseDate:   releaseDate,
            PosterUrl:     d.SafePoster,
            BackdropUrl:   null,  // OMDb doesn't provide backdrop images
            Rating:        rating,
            VoteCount:     voteCount,
            Genres:        genres,
            ExternalIds:   externalIds,
            IsAdult:       false);
    }

    private static AddonMetadataResult MapSearchEntry(OmdbSearchEntry e)
    {
        var externalIds = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(e.ImdbId)) externalIds["imdb"] = e.ImdbId!;

        DateOnly? releaseDate = null;
        if (e.Year?.Length >= 4 && int.TryParse(e.Year.AsSpan(0, 4), out var yr))
            releaseDate = new DateOnly(yr, 1, 1);

        return new AddonMetadataResult(
            ProviderId:    e.ImdbId ?? string.Empty,
            Title:         e.Title ?? "Untitled",
            OriginalTitle: null,
            Overview:      null,
            ReleaseDate:   releaseDate,
            PosterUrl:     e.Poster == "N/A" ? null : e.Poster,
            BackdropUrl:   null,
            Rating:        null,
            VoteCount:     null,
            Genres:        [],
            ExternalIds:   externalIds);
    }

    /// <summary>Build a OMDb API URL with the API key and given parameters.</summary>
    private string BuildUrl(params (string key, string value)[] args)
    {
        var sb = new StringBuilder("?apikey=");
        sb.Append(Uri.EscapeDataString(_apiKey));
        foreach (var (key, value) in args)
        {
            sb.Append('&');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }
        return sb.ToString();
    }

    private static string NormalizeKind(AddonMediaKind kind) =>
        kind == AddonMediaKind.Series ? "series" : "movie";

    private static TimeSpan TryParseMinutes(string? raw, int defaultMinutes)
    {
        if (int.TryParse(raw, out var minutes) && minutes > 0)
            return TimeSpan.FromMinutes(minutes);
        return TimeSpan.FromMinutes(defaultMinutes);
    }

    private static TimeSpan TryParseHours(string? raw, int defaultHours)
    {
        if (int.TryParse(raw, out var hours) && hours > 0)
            return TimeSpan.FromHours(hours);
        return TimeSpan.FromHours(defaultHours);
    }
}
