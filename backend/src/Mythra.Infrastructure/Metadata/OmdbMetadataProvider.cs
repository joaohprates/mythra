using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

/// <summary>
/// Metadata provider backed by the OMDb API (Open Movie Database), which is
/// powered by IMDb data. ProviderId returned by this provider IS the IMDb ID
/// (e.g. "tt0133093"), so ExternalIds["imdb"] is always populated.
///
/// Instances are created by AddonActivator — each addon entity gets its own
/// instance with its own API key and cache namespace.
/// </summary>
public sealed class OmdbMetadataProvider : IMetadataProvider
{
    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly string _cachePrefix;
    private readonly TimeSpan _searchTtl;
    private readonly TimeSpan _detailTtl;
    private readonly ILogger _log;

    private const string BaseUrl = "https://www.omdbapi.com/";

    public string Name => "omdb";
    public bool Supports(MediaKind kind) => kind == MediaKind.Video;

    public OmdbMetadataProvider(
        string apiKey,
        IHttpClientFactory httpFactory,
        IMemoryCache cache,
        string cachePrefix,
        TimeSpan searchTtl,
        TimeSpan detailTtl,
        ILogger logger)
    {
        _apiKey = apiKey;
        _httpFactory = httpFactory;
        _cache = cache;
        _cachePrefix = cachePrefix;
        _searchTtl = searchTtl;
        _detailTtl = detailTtl;
        _log = logger;
    }

    // ── IMetadataProvider ─────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(query)) return [];

        var cacheKey = $"{_cachePrefix}search:{query.ToLowerInvariant()}:{year}";
        if (_cache.TryGetValue<List<MetadataSearchResult>>(cacheKey, out var hit) && hit is not null)
            return hit;

        var url = BuildUrl(("s", query));
        if (year.HasValue) url += $"&y={year}";

        OmdbSearchResponse? resp;
        using var http = CreateClient();
        try { resp = await http.GetFromJsonAsync<OmdbSearchResponse>(url, OmdbJsonOptions, ct); }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[OMDb] Search failed for '{Query}'", query);
            return [];
        }

        if (resp is null || !resp.IsSuccess || resp.Search is null)
        {
            _log.LogDebug("[OMDb] No results for '{Query}': {Error}", query, resp?.Error);
            return [];
        }

        // Enrich the top 3 results with full details; stub the rest.
        var results = new List<MetadataSearchResult>();
        for (int i = 0; i < resp.Search.Count; i++)
        {
            var entry = resp.Search[i];
            if (string.IsNullOrWhiteSpace(entry.ImdbId)) continue;

            if (i < 3)
            {
                var detail = await FetchDetailAsync(entry.ImdbId, ct);
                if (detail is not null) { results.Add(detail); continue; }
            }

            results.Add(StubFromEntry(entry));
        }

        _cache.Set(cacheKey, results, _searchTtl);
        return results;
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(
        string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(providerId)) return null;
        return await FetchDetailAsync(providerId, ct);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private async Task<MetadataSearchResult?> FetchDetailAsync(string imdbId, CancellationToken ct)
    {
        var cacheKey = $"{_cachePrefix}detail:{imdbId}";
        if (_cache.TryGetValue<MetadataSearchResult>(cacheKey, out var hit) && hit is not null)
            return hit;

        using var http = CreateClient();
        OmdbDetailResponse? detail;
        try { detail = await http.GetFromJsonAsync<OmdbDetailResponse>(BuildUrl(("i", imdbId), ("plot", "full")), OmdbJsonOptions, ct); }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[OMDb] Detail fetch failed for {Id}", imdbId);
            return null;
        }

        if (detail is null || !detail.IsSuccess)
        {
            _log.LogDebug("[OMDb] Not found: {Id} — {Error}", imdbId, detail?.Error);
            return null;
        }

        var result = MapDetail(detail);
        _cache.Set(cacheKey, result, _detailTtl);
        return result;
    }

    private MetadataSearchResult MapDetail(OmdbDetailResponse d)
    {
        var genres = d.NullIfNa(d.Genre)?
            .Split(',', StringSplitOptions.TrimEntries)
            .Where(g => !string.IsNullOrEmpty(g))
            .ToList() ?? [];

        var providerIds = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(d.ImdbId)) providerIds["imdb"] = d.ImdbId!;
        if (!string.IsNullOrWhiteSpace(d.ImdbId)) providerIds["omdb"] = d.ImdbId!;

        double? rating = null;
        if (double.TryParse(d.NullIfNa(d.ImdbRating),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var r))
            rating = r;

        DateOnly? releaseDate = null;
        if (!string.IsNullOrWhiteSpace(d.NullIfNa(d.Released))
            && DateOnly.TryParse(d.Released, out var dt))
            releaseDate = dt;
        else if (!string.IsNullOrWhiteSpace(d.Year) && d.Year.Length >= 4
            && int.TryParse(d.Year[..4], out var yr))
            releaseDate = new DateOnly(yr, 1, 1);

        return new MetadataSearchResult(
            ProviderId:    d.ImdbId ?? string.Empty,
            Title:         d.NullIfNa(d.Title) ?? "Untitled",
            OriginalTitle: null,
            Overview:      d.NullIfNa(d.Plot),
            ReleaseDate:   releaseDate,
            PosterUrl:     d.NullIfNa(d.Poster),
            BackdropUrl:   null,
            Rating:        rating,
            Genres:        genres,
            ProviderIds:   providerIds);
    }

    private static MetadataSearchResult StubFromEntry(OmdbSearchEntry e)
    {
        var ids = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(e.ImdbId)) { ids["imdb"] = e.ImdbId!; ids["omdb"] = e.ImdbId!; }

        DateOnly? releaseDate = null;
        if (e.Year?.Length >= 4 && int.TryParse(e.Year[..4], out var yr))
            releaseDate = new DateOnly(yr, 1, 1);

        return new MetadataSearchResult(
            ProviderId:    e.ImdbId ?? string.Empty,
            Title:         e.Title ?? "Untitled",
            OriginalTitle: null,
            Overview:      null,
            ReleaseDate:   releaseDate,
            PosterUrl:     e.Poster == "N/A" ? null : e.Poster,
            BackdropUrl:   null,
            Rating:        null,
            Genres:        [],
            ProviderIds:   ids);
    }

    private HttpClient CreateClient()
    {
        var client = _httpFactory.CreateClient("OmdbMetadata");
        client.BaseAddress = new Uri(BaseUrl);
        return client;
    }

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

    private static readonly JsonSerializerOptions OmdbJsonOptions =
        new(JsonSerializerDefaults.Web);

    // ── OMDb response DTOs ────────────────────────────────────────────────────

    private sealed class OmdbSearchResponse
    {
        [JsonPropertyName("Search")]   public List<OmdbSearchEntry>? Search   { get; init; }
        [JsonPropertyName("Response")] public string? Response                { get; init; }
        [JsonPropertyName("Error")]    public string? Error                   { get; init; }
        public bool IsSuccess => string.Equals(Response, "True", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class OmdbSearchEntry
    {
        [JsonPropertyName("imdbID")]  public string? ImdbId { get; init; }
        [JsonPropertyName("Title")]   public string? Title  { get; init; }
        [JsonPropertyName("Year")]    public string? Year   { get; init; }
        [JsonPropertyName("Poster")]  public string? Poster { get; init; }
        [JsonPropertyName("Type")]    public string? Type   { get; init; }
    }

    private sealed class OmdbDetailResponse
    {
        [JsonPropertyName("imdbID")]     public string? ImdbId     { get; init; }
        [JsonPropertyName("Title")]      public string? Title      { get; init; }
        [JsonPropertyName("Year")]       public string? Year       { get; init; }
        [JsonPropertyName("Released")]   public string? Released   { get; init; }
        [JsonPropertyName("Plot")]       public string? Plot       { get; init; }
        [JsonPropertyName("Poster")]     public string? Poster     { get; init; }
        [JsonPropertyName("Genre")]      public string? Genre      { get; init; }
        [JsonPropertyName("imdbRating")] public string? ImdbRating { get; init; }
        [JsonPropertyName("Response")]   public string? Response   { get; init; }
        [JsonPropertyName("Error")]      public string? Error      { get; init; }
        public bool IsSuccess => string.Equals(Response, "True", StringComparison.OrdinalIgnoreCase);

        // OMDb returns "N/A" for missing fields — treat as null.
        public string? NullIfNa(string? s) =>
            string.IsNullOrWhiteSpace(s) || s.Equals("N/A", StringComparison.OrdinalIgnoreCase) ? null : s;
    }
}
