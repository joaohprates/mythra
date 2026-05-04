using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

public sealed class AniListMetadataProvider(
    HttpClient http,
    IMemoryCache cache,
    IOptions<MetadataOptions> opts,
    ILogger<AniListMetadataProvider> log) : IMetadataProvider
{
    private readonly MetadataOptions _opts = opts.Value;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public string Name => "anilist";

    public bool Supports(MediaKind kind) => kind == MediaKind.Video || kind == MediaKind.Manga;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind)) return [];

        var type = kind == MediaKind.Manga ? "MANGA" : "ANIME";
        var graphql = new
        {
            query = """
                query ($search: String, $type: MediaType, $year: Int) {
                  Page(perPage: 25) {
                    media(search: $search, type: $type, seasonYear: $year) {
                      id
                      idMal
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
            variables = new { search = query, type, year }
        };

        try
        {
            var jsonStr = await PostCachedAsync($"search:{type}:{query}:{year}", graphql, ct);
            if (jsonStr is null) return [];
            var json = JsonSerializer.Deserialize<JsonElement>(jsonStr);
            var media = json.GetProperty("data").GetProperty("Page").GetProperty("media");
            var results = new List<MetadataSearchResult>();
            foreach (var m in media.EnumerateArray()) results.Add(Map(m));
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "AniList search failed for {Query}", query);
            return [];
        }
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind) || !int.TryParse(providerId, out var id)) return null;

        var type = kind == MediaKind.Manga ? "MANGA" : "ANIME";
        var graphql = new
        {
            query = """
                query ($id: Int, $type: MediaType) {
                  Media(id: $id, type: $type) {
                    id idMal
                    title { romaji english native }
                    description(asHtml: false)
                    startDate { year month day }
                    coverImage { extraLarge } bannerImage
                    averageScore genres isAdult
                  }
                }
                """,
            variables = new { id, type }
        };

        try
        {
            var jsonStr = await PostCachedAsync($"id:{type}:{id}", graphql, ct);
            if (jsonStr is null) return null;
            var json = JsonSerializer.Deserialize<JsonElement>(jsonStr);
            return Map(json.GetProperty("data").GetProperty("Media"));
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "AniList fetch failed for {Id}", providerId);
            return null;
        }
    }

    private async Task<string?> PostCachedAsync(string cacheSuffix, object graphql, CancellationToken ct)
    {
        var key = $"anilist:{cacheSuffix}";
        if (cache.TryGetValue(key, out string? json) && json is not null) return json;
        var response = await http.PostAsJsonAsync(_opts.AniListBaseUrl, graphql, ct);
        if (!response.IsSuccessStatusCode) return null;
        json = await response.Content.ReadAsStringAsync(ct);
        cache.Set(key, json, CacheTtl);
        return json;
    }

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
            && sd.TryGetProperty("year", out var yr) && yr.ValueKind == JsonValueKind.Number
            && sd.TryGetProperty("month", out var mo) && mo.ValueKind == JsonValueKind.Number
            && sd.TryGetProperty("day", out var dy) && dy.ValueKind == JsonValueKind.Number)
        {
            releaseDate = new DateOnly(yr.GetInt32(), Math.Max(mo.GetInt32(), 1), Math.Max(dy.GetInt32(), 1));
        }
        var cover = m.TryGetProperty("coverImage", out var ci) && ci.TryGetProperty("extraLarge", out var xl) ? xl.GetString() : null;
        var banner = m.TryGetProperty("bannerImage", out var bi) && bi.ValueKind == JsonValueKind.String ? bi.GetString() : null;
        var rating = m.TryGetProperty("averageScore", out var avg) && avg.ValueKind == JsonValueKind.Number ? avg.GetDouble() / 10.0 : (double?)null;
        var genres = m.TryGetProperty("genres", out var gs)
            ? gs.EnumerateArray().Select(g => g.GetString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList()
            : [];

        var isAdult = m.TryGetProperty("isAdult", out var ia) && ia.ValueKind == JsonValueKind.True;
        // Also detect adult content from genres
        if (!isAdult && genres.Any(g => g is "Hentai" or "Ecchi" or "Adult"))
            isAdult = true;

        var providerIds = new Dictionary<string, string> { ["anilist"] = m.GetProperty("id").GetInt32().ToString() };
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
