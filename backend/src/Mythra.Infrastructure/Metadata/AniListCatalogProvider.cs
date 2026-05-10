using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

/// <summary>
/// AniList-backed catalog browsing for anime/manga. Used as the discover fallback when
/// the user picks the "anime" tab — Cinemeta only covers live-action movies/series.
/// </summary>
public sealed class AniListCatalogProvider(
    IHttpClientFactory httpFactory,
    IOptions<MetadataOptions> opts,
    ILogger<AniListCatalogProvider> log) : ICatalogProvider
{
    private readonly MetadataOptions _opts = opts.Value;

    public string Name => "anilist-catalog";

    public bool SupportsCatalog(MediaKind kind, string catalogType) =>
        catalogType is "anime" or "manga"
        && (kind == MediaKind.Video || kind == MediaKind.Manga);

    public async Task<IReadOnlyList<MetadataSearchResult>> GetCatalogAsync(
        MediaKind kind, string catalogType, string category, int skip, int take, string? genre = null, CancellationToken ct = default)
    {
        if (!SupportsCatalog(kind, catalogType)) return [];

        var perPage = Math.Clamp(take, 1, 50);
        var page    = (skip / perPage) + 1;
        var sort    = NormalizeSort(category);
        var type    = catalogType == "manga" ? "MANGA" : "ANIME";

        var graphql = new
        {
            query = """
                query ($page: Int, $perPage: Int, $type: MediaType, $sort: [MediaSort], $genre: String) {
                  Page(page: $page, perPage: $perPage) {
                    media(type: $type, sort: $sort, genre: $genre) {
                      id idMal
                      title { romaji english native }
                      description(asHtml: false)
                      startDate { year month day }
                      coverImage { extraLarge }
                      bannerImage
                      averageScore
                      genres
                      isAdult
                    }
                  }
                }
                """,
            variables = new { page, perPage, type, sort = new[] { sort }, genre = string.IsNullOrWhiteSpace(genre) ? (string?)null : genre }
        };

        try
        {
            using var http = httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            var response = await http.PostAsJsonAsync(_opts.AniListBaseUrl, graphql, ct);
            if (!response.IsSuccessStatusCode) return [];
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            var media = json.GetProperty("data").GetProperty("Page").GetProperty("media");
            var results = new List<MetadataSearchResult>();
            foreach (var m in media.EnumerateArray()) results.Add(Map(m));
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "AniList catalog fetch failed for {Type}/{Category}", catalogType, category);
            return [];
        }
    }

    private static string NormalizeSort(string category) => category?.ToLowerInvariant() switch
    {
        "trending"  => "TRENDING_DESC",
        "rating" or "top" or "imdbrating" => "SCORE_DESC",
        "year"      => "START_DATE_DESC",
        _ => "POPULARITY_DESC",
    };

    private static MetadataSearchResult Map(JsonElement m)
    {
        var titleObj = m.GetProperty("title");
        var title = titleObj.TryGetProperty("english", out var en) && en.ValueKind == JsonValueKind.String
            ? en.GetString()!
            : titleObj.GetProperty("romaji").GetString() ?? "Unknown";
        var original = titleObj.TryGetProperty("native", out var native) ? native.GetString() : null;
        var overview = m.TryGetProperty("description", out var d) ? d.GetString() : null;

        DateOnly? releaseDate = null;
        if (m.TryGetProperty("startDate", out var sd)
            && sd.TryGetProperty("year", out var yr) && yr.ValueKind == JsonValueKind.Number)
        {
            var month = sd.TryGetProperty("month", out var mo) && mo.ValueKind == JsonValueKind.Number ? Math.Max(mo.GetInt32(), 1) : 1;
            var day   = sd.TryGetProperty("day", out var dy) && dy.ValueKind == JsonValueKind.Number   ? Math.Max(dy.GetInt32(), 1) : 1;
            releaseDate = new DateOnly(yr.GetInt32(), month, day);
        }

        var cover = m.TryGetProperty("coverImage", out var ci) && ci.TryGetProperty("extraLarge", out var xl) ? xl.GetString() : null;
        var banner = m.TryGetProperty("bannerImage", out var bi) && bi.ValueKind == JsonValueKind.String ? bi.GetString() : null;
        var rating = m.TryGetProperty("averageScore", out var avg) && avg.ValueKind == JsonValueKind.Number ? avg.GetDouble() / 10.0 : (double?)null;

        var genres = m.TryGetProperty("genres", out var gs)
            ? gs.EnumerateArray().Select(g => g.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
            : [];

        var isAdult = m.TryGetProperty("isAdult", out var ia) && ia.ValueKind == JsonValueKind.True;
        if (!isAdult && genres.Any(g => g is "Hentai" or "Ecchi" or "Adult"))
            isAdult = true;

        var providerIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["anilist"] = m.GetProperty("id").GetInt32().ToString(),
        };
        if (m.TryGetProperty("idMal", out var mal) && mal.ValueKind == JsonValueKind.Number)
            providerIds["mal"] = mal.GetInt32().ToString();

        return new MetadataSearchResult(
            ProviderId: m.GetProperty("id").GetInt32().ToString(),
            Title: title,
            OriginalTitle: original,
            Overview: overview,
            ReleaseDate: releaseDate,
            PosterUrl: cover,
            BackdropUrl: banner,
            Rating: rating,
            Genres: genres,
            ProviderIds: providerIds,
            IsAdult: isAdult);
    }
}
