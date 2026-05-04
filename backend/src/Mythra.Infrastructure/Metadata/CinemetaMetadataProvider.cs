using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

/// <summary>
/// Stremio Cinemeta provider — Stremio's open metadata API for movies and series.
/// Endpoints used:
///   GET /catalog/{type}/{id}.json                    → catalog browsing
///   GET /catalog/{type}/{id}/skip={n}.json           → paginated browsing
///   GET /catalog/{type}/{id}/search={query}.json     → free-text search
///   GET /meta/{type}/{imdbId}.json                   → detail lookup by IMDb id
/// </summary>
public sealed class CinemetaMetadataProvider(
    HttpClient http,
    IMemoryCache cache,
    ILogger<CinemetaMetadataProvider> log) : IMetadataProvider, ICatalogProvider
{
    public const string ProviderName = "cinemeta";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public string Name => ProviderName;

    public bool Supports(MediaKind kind) => kind == MediaKind.Video;

    public bool SupportsCatalog(MediaKind kind, string catalogType) =>
        kind == MediaKind.Video && (catalogType is "movie" or "series");

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(query)) return [];

        // Cinemeta has no "multi" endpoint — query both movie and series and merge results.
        var movies  = await FetchAsync($"catalog/movie/top/search={Uri.EscapeDataString(query)}.json", "movie", ct);
        var series  = await FetchAsync($"catalog/series/top/search={Uri.EscapeDataString(query)}.json", "series", ct);

        var combined = movies.Concat(series).ToList();

        if (year.HasValue)
            combined = combined.Where(r => r.ReleaseDate?.Year == year.Value).ToList();

        return combined;
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(providerId)) return null;

        // providerId format: "movie:tt1234567" or "series:tt7654321".
        // Fall back to bare IMDb id (assume movie) for resilience.
        string type = "movie";
        string imdb = providerId;
        var sep = providerId.IndexOf(':');
        if (sep > 0)
        {
            type = providerId[..sep];
            imdb = providerId[(sep + 1)..];
        }
        if (type is not ("movie" or "series")) return null;

        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>($"meta/{type}/{imdb}.json", ct);
            if (!json.TryGetProperty("meta", out var meta)) return null;
            return MapMeta(meta, type);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Cinemeta detail fetch failed for {Id}", providerId);
            return null;
        }
    }

    public async Task<IReadOnlyList<MetadataSearchResult>> GetCatalogAsync(
        MediaKind kind, string catalogType, string category, int skip, int take, CancellationToken ct = default)
    {
        if (!SupportsCatalog(kind, catalogType)) return [];

        // Cinemeta supports these catalog ids out of the box: top, year, imdbRating, popular (alias for top).
        var catalogId = NormalizeCategory(category);

        // Stremio paginates in steps of 100. We fetch one page and slice locally.
        var pageStart = (skip / 100) * 100;
        var url = pageStart == 0
            ? $"catalog/{catalogType}/{catalogId}.json"
            : $"catalog/{catalogType}/{catalogId}/skip={pageStart}.json";

        var page = await FetchAsync(url, catalogType, ct);
        var localOffset = skip - pageStart;
        return page.Skip(localOffset).Take(take).ToList();
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string NormalizeCategory(string category) => category?.ToLowerInvariant() switch
    {
        null or "" or "popular" or "trending" => "top",
        "top"        => "top",
        "year"       => "year",
        "rating"     => "imdbRating",
        "imdbrating" => "imdbRating",
        _            => "top",
    };

    private async Task<List<MetadataSearchResult>> FetchAsync(string relativeUrl, string type, CancellationToken ct)
    {
        var cacheKey = $"cinemeta:{relativeUrl}";
        if (cache.TryGetValue<List<MetadataSearchResult>>(cacheKey, out var cached) && cached is not null)
            return cached;

        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(relativeUrl, ct);
            if (!json.TryGetProperty("metas", out var metas) || metas.ValueKind != JsonValueKind.Array)
                return [];

            var results = new List<MetadataSearchResult>(metas.GetArrayLength());
            foreach (var m in metas.EnumerateArray())
                results.Add(MapMeta(m, type));

            cache.Set(cacheKey, results, CacheTtl);
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Cinemeta fetch failed for {Url}", relativeUrl);
            return [];
        }
    }

    private static MetadataSearchResult MapMeta(JsonElement m, string type)
    {
        var imdb = TryGetString(m, "id") ?? "";
        var title = TryGetString(m, "name") ?? "Untitled";
        var overview = TryGetString(m, "description");
        var poster = TryGetString(m, "poster");
        var background = TryGetString(m, "background");
        DateOnly? releaseDate = null;
        if (TryGetString(m, "released") is { } released && DateTime.TryParse(released, out var dt))
            releaseDate = DateOnly.FromDateTime(dt);
        else if (TryGetString(m, "year") is { } yearStr && int.TryParse(yearStr.Split('-', '–')[0], out var yr))
            releaseDate = new DateOnly(yr, 1, 1);

        double? rating = null;
        if (m.TryGetProperty("imdbRating", out var ir))
        {
            if (ir.ValueKind == JsonValueKind.Number) rating = ir.GetDouble();
            else if (ir.ValueKind == JsonValueKind.String && double.TryParse(ir.GetString(), out var parsed)) rating = parsed;
        }

        var genres = new List<string>();
        if (m.TryGetProperty("genres", out var gs) && gs.ValueKind == JsonValueKind.Array)
            foreach (var g in gs.EnumerateArray())
                if (g.ValueKind == JsonValueKind.String && g.GetString() is { Length: > 0 } gname)
                    genres.Add(gname);

        var providerIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(imdb)) providerIds["imdb"] = imdb;
        providerIds["cinemeta"] = $"{type}:{imdb}";

        return new MetadataSearchResult(
            ProviderId:    $"{type}:{imdb}",
            Title:         title,
            OriginalTitle: null,
            Overview:      overview,
            ReleaseDate:   releaseDate,
            PosterUrl:     poster,
            BackdropUrl:   background,
            Rating:        rating,
            Genres:        genres,
            ProviderIds:   providerIds,
            IsAdult:       false);
    }

    private static string? TryGetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
