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
    ILogger<TmdbMetadataProvider> log) : IMetadataProvider, ICatalogProvider
{
    private readonly MetadataOptions _opts = opts.Value;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public string Name => "tmdb";

    public bool Supports(MediaKind kind) => kind == MediaKind.Video;

    // ── ICatalogProvider ─────────────────────────────────────────────────────

    public bool SupportsCatalog(MediaKind kind, string catalogType) =>
        !string.IsNullOrWhiteSpace(_opts.TmdbApiKey)
        && kind == MediaKind.Video
        && catalogType is "movie" or "series";

    public async Task<IReadOnlyList<MetadataSearchResult>> GetCatalogAsync(
        MediaKind kind, string catalogType, string category, int skip, int take,
        string? genre = null, CancellationToken ct = default)
    {
        if (!SupportsCatalog(kind, catalogType)) return [];

        var mediaType = catalogType == "series" ? "tv" : "movie";
        var page = Math.Max(1, skip / Math.Max(take, 1) + 1);

        string url;
        if (!string.IsNullOrWhiteSpace(genre))
        {
            var genreMap = mediaType == "tv" ? TvGenreIds : MovieGenreIds;
            var genreId = genreMap.TryGetValue(genre.Trim(), out var id) ? id : 0;
            if (genreId == 0)
            {
                // Try case-insensitive partial match
                var match = genreMap.FirstOrDefault(kv => kv.Key.Contains(genre.Trim(), StringComparison.OrdinalIgnoreCase));
                genreId = match.Value;
            }
            if (genreId > 0)
                url = $"discover/{mediaType}?api_key={_opts.TmdbApiKey}&language=en-US&with_genres={genreId}&sort_by=popularity.desc&page={page}";
            else
                url = $"discover/{mediaType}?api_key={_opts.TmdbApiKey}&language=en-US&with_keywords={Uri.EscapeDataString(genre)}&sort_by=popularity.desc&page={page}";
        }
        else
        {
            url = category?.ToLowerInvariant() switch
            {
                "trending" => $"trending/{mediaType}/week?api_key={_opts.TmdbApiKey}&language=en-US&page={page}",
                "rating" or "top" => $"{mediaType}/top_rated?api_key={_opts.TmdbApiKey}&language=en-US&page={page}",
                _ => $"{mediaType}/popular?api_key={_opts.TmdbApiKey}&language=en-US&page={page}",
            };
        }

        try
        {
            // Fetch two consecutive pages in parallel for double the results.
            var url2 = url.Replace($"&page={page}", $"&page={page + 1}");
            var task1 = GetJsonCachedAsync(url, ct);
            var task2 = GetJsonCachedAsync(url2, ct);
            await Task.WhenAll(task1, task2);

            var results = new List<TmdbResult>();
            foreach (var json in new[] { task1.Result, task2.Result })
            {
                if (json is null) continue;
                var response = JsonSerializer.Deserialize<TmdbPagedResponse>(json);
                if (response?.Results is not null) results.AddRange(response.Results);
            }

            var mediaKind = mediaType == "tv" ? "tv" : "movie";
            return results.DistinctBy(r => r.Id).Select(r => MapResult(r, mediaKind)).ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "TMDb catalog fetch failed ({Type}/{Category})", mediaType, category);
            return [];
        }
    }

    private static readonly Dictionary<string, int> MovieGenreIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["action"] = 28, ["adventure"] = 12, ["animation"] = 16, ["comedy"] = 35,
        ["crime"] = 80, ["documentary"] = 99, ["drama"] = 18, ["family"] = 10751,
        ["fantasy"] = 14, ["history"] = 36, ["horror"] = 27, ["music"] = 10402,
        ["mystery"] = 9648, ["romance"] = 10749, ["sci-fi"] = 878, ["science fiction"] = 878,
        ["thriller"] = 53, ["war"] = 10752, ["western"] = 37,
    };

    private static readonly Dictionary<string, int> TvGenreIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["action"] = 10759, ["adventure"] = 10759, ["animation"] = 16, ["comedy"] = 35,
        ["crime"] = 80, ["documentary"] = 99, ["drama"] = 18, ["family"] = 10751,
        ["kids"] = 10762, ["mystery"] = 9648, ["reality"] = 10764, ["sci-fi"] = 10765,
        ["science fiction"] = 10765, ["soap"] = 10766, ["western"] = 37,
    };

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opts.TmdbApiKey))
        {
            log.LogDebug("TMDb key not configured — skipping search.");
            return [];
        }
        if (!Supports(kind)) return [];

        // Fetch two pages and merge for better search coverage (TMDb returns 20/page).
        var baseUrl = $"search/multi?api_key={_opts.TmdbApiKey}&query={Uri.EscapeDataString(query)}&include_adult=true&language=en-US";
        if (year.HasValue) baseUrl += $"&year={year}";

        try
        {
            var page1Task = GetJsonCachedAsync($"{baseUrl}&page=1", ct);
            var page2Task = GetJsonCachedAsync($"{baseUrl}&page=2", ct);
            await Task.WhenAll(page1Task, page2Task);

            var results = new List<TmdbResult>();
            foreach (var json in new[] { page1Task.Result, page2Task.Result })
            {
                if (json is null) continue;
                var response = JsonSerializer.Deserialize<TmdbSearchResponse>(json);
                if (response?.Results is not null)
                    results.AddRange(response.Results);
            }

            return results
                .DistinctBy(r => r.Id)
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

        var url = $"{type}/{id}?api_key={_opts.TmdbApiKey}&language=en-US&append_to_response=external_ids,credits";
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

    private MetadataSearchResult MapResult(TmdbResult r) =>
        MapResult(r, r.MediaType ?? "movie");

    private MetadataSearchResult MapResult(TmdbResult r, string type)
    {
        var title = r.Title ?? r.Name ?? "Untitled";
        var release = r.ReleaseDate ?? r.FirstAirDate;
        DateOnly? releaseDate = !string.IsNullOrEmpty(release) && DateOnly.TryParse(release, out var d) ? d : null;
        var providerIds = new Dictionary<string, string>
        {
            ["tmdb"] = $"{type}:{r.Id}",
        };
        if (!string.IsNullOrEmpty(r.MediaType))
            providerIds["tmdb"] = $"{r.MediaType}:{r.Id}";
        return new MetadataSearchResult(
            ProviderId: $"{type}:{r.Id}",
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

        var cast = new List<MetadataCastMember>();
        if (json.TryGetProperty("credits", out var credits))
        {
            if (credits.TryGetProperty("cast", out var castArr))
            {
                var order = 0;
                foreach (var actor in castArr.EnumerateArray().Take(15))
                {
                    var aName = actor.TryGetProperty("name", out var an) ? an.GetString() : null;
                    if (string.IsNullOrEmpty(aName)) continue;
                    var character = actor.TryGetProperty("character", out var ch) ? ch.GetString() : null;
                    var tmdbPid = actor.TryGetProperty("id", out var aid) ? aid.GetInt32().ToString() : null;
                    cast.Add(new MetadataCastMember(aName, PersonRole.Actor, character, order++, tmdbPid));
                }
            }
            if (credits.TryGetProperty("crew", out var crewArr))
            {
                foreach (var member in crewArr.EnumerateArray())
                {
                    var job = member.TryGetProperty("job", out var j) ? j.GetString() : null;
                    if (job is not ("Director" or "Screenplay" or "Writer")) continue;
                    var cName = member.TryGetProperty("name", out var cn) ? cn.GetString() : null;
                    if (string.IsNullOrEmpty(cName)) continue;
                    var tmdbPid = member.TryGetProperty("id", out var mid) ? mid.GetInt32().ToString() : null;
                    var role = job is "Director" ? PersonRole.Director : PersonRole.Writer;
                    cast.Add(new MetadataCastMember(cName, role, null, 100 + cast.Count, tmdbPid));
                }
            }
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
            ProviderIds: providerIds,
            Cast: cast.Count > 0 ? cast : null);
    }

    private sealed class TmdbSearchResponse { public List<TmdbResult>? Results { get; set; } }
    private sealed class TmdbPagedResponse { public List<TmdbResult>? Results { get; set; } public int TotalPages { get; set; } }
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
