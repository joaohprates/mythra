using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

public sealed class TmdbMetadataProvider(
    HttpClient http,
    IMemoryCache cache,
    IOptions<MetadataOptions> opts,
    ILogger<TmdbMetadataProvider> log) : IMetadataProvider
{
    private readonly MetadataOptions _opts = opts.Value;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public string Name => "tmdb";

    public bool Supports(MediaKind kind) => kind == MediaKind.Video;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.TmdbApiKey))
        {
            log.LogDebug("TMDb key not configured — skipping search.");
            return [];
        }
        if (!Supports(kind)) return [];

        var url = $"search/multi?api_key={_opts.TmdbApiKey}&query={Uri.EscapeDataString(query)}&include_adult=false&language=en-US";
        if (year.HasValue) url += $"&year={year}";

        try
        {
            var json = await GetJsonCachedAsync(url, ct);
            if (json is null) return [];
            var response = JsonSerializer.Deserialize<TmdbSearchResponse>(json);
            if (response?.Results is null) return [];
            return response.Results
                .Where(r => r.MediaType is "movie" or "tv")
                .Select(MapResult)
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "TMDb search failed for {Query}", query);
            return [];
        }
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.TmdbApiKey)) return null;
        if (!Supports(kind)) return null;

        // providerId format: "movie:123" or "tv:456"
        var parts = providerId.Split(':');
        if (parts.Length != 2) return null;
        var (type, id) = (parts[0], parts[1]);

        var url = $"{type}/{id}?api_key={_opts.TmdbApiKey}&language=en-US&append_to_response=external_ids";
        try
        {
            var jsonStr = await GetJsonCachedAsync(url, ct);
            if (jsonStr is null) return null;
            var json = JsonSerializer.Deserialize<JsonElement>(jsonStr);
            return MapDetail(json, type);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "TMDb fetch failed for {Id}", providerId);
            return null;
        }
    }

    private async Task<string?> GetJsonCachedAsync(string url, CancellationToken ct)
    {
        var key = $"tmdb:{url}";
        if (cache.TryGetValue(key, out string? json) && json is not null) return json;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        json = await resp.Content.ReadAsStringAsync(ct);
        cache.Set(key, json, CacheTtl);
        return json;
    }

    private MetadataSearchResult MapResult(TmdbResult r)
    {
        var title = r.Title ?? r.Name ?? "Untitled";
        var release = r.ReleaseDate ?? r.FirstAirDate;
        DateOnly? releaseDate = !string.IsNullOrEmpty(release) && DateOnly.TryParse(release, out var d) ? d : null;
        var providerIds = new Dictionary<string, string>
        {
            ["tmdb"] = $"{r.MediaType}:{r.Id}",
        };
        return new MetadataSearchResult(
            ProviderId: $"{r.MediaType}:{r.Id}",
            Title: title,
            OriginalTitle: r.OriginalTitle ?? r.OriginalName,
            Overview: r.Overview,
            ReleaseDate: releaseDate,
            PosterUrl: !string.IsNullOrEmpty(r.PosterPath) ? $"{_opts.TmdbImageBaseUrl}{r.PosterPath}" : null,
            BackdropUrl: !string.IsNullOrEmpty(r.BackdropPath) ? $"{_opts.TmdbImageBaseUrl}{r.BackdropPath}" : null,
            Rating: r.VoteAverage,
            Genres: [],
            ProviderIds: providerIds);
    }

    private MetadataSearchResult MapDetail(JsonElement json, string type)
    {
        var title = json.TryGetProperty("title", out var t) ? t.GetString()
                  : json.TryGetProperty("name", out var n) ? n.GetString()
                  : "Untitled";
        var overview = json.TryGetProperty("overview", out var o) ? o.GetString() : null;
        var release = json.TryGetProperty("release_date", out var rd) ? rd.GetString()
                    : json.TryGetProperty("first_air_date", out var fad) ? fad.GetString()
                    : null;
        DateOnly? releaseDate = !string.IsNullOrEmpty(release) && DateOnly.TryParse(release, out var dt) ? dt : null;
        var poster = json.TryGetProperty("poster_path", out var pp) ? pp.GetString() : null;
        var backdrop = json.TryGetProperty("backdrop_path", out var bp) ? bp.GetString() : null;
        var rating = json.TryGetProperty("vote_average", out var va) && va.ValueKind == JsonValueKind.Number ? va.GetDouble() : (double?)null;
        var id = json.GetProperty("id").GetInt32();
        var genres = json.TryGetProperty("genres", out var gs)
            ? gs.EnumerateArray().Select(g => g.GetProperty("name").GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
            : [];

        var providerIds = new Dictionary<string, string> { ["tmdb"] = $"{type}:{id}" };
        if (json.TryGetProperty("external_ids", out var ext) && ext.TryGetProperty("imdb_id", out var imdb) && imdb.ValueKind == JsonValueKind.String)
        {
            var imdbId = imdb.GetString();
            if (!string.IsNullOrEmpty(imdbId)) providerIds["imdb"] = imdbId;
        }

        return new MetadataSearchResult(
            ProviderId: $"{type}:{id}",
            Title: title ?? "Untitled",
            OriginalTitle: null,
            Overview: overview,
            ReleaseDate: releaseDate,
            PosterUrl: !string.IsNullOrEmpty(poster) ? $"{_opts.TmdbImageBaseUrl}{poster}" : null,
            BackdropUrl: !string.IsNullOrEmpty(backdrop) ? $"{_opts.TmdbImageBaseUrl}{backdrop}" : null,
            Rating: rating,
            Genres: genres,
            ProviderIds: providerIds);
    }

    private sealed class TmdbSearchResponse { public List<TmdbResult>? Results { get; set; } }
    private sealed class TmdbResult
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Name { get; set; }
        public string? OriginalTitle { get; set; }
        public string? OriginalName { get; set; }
        public string? Overview { get; set; }
        public string? ReleaseDate { get; set; }
        public string? FirstAirDate { get; set; }
        public string? PosterPath { get; set; }
        public string? BackdropPath { get; set; }
        public double? VoteAverage { get; set; }
        public string? MediaType { get; set; }
    }
}
