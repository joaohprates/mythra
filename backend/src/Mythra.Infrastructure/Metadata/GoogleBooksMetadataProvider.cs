using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

public sealed class GoogleBooksMetadataProvider(
    HttpClient http,
    ILogger<GoogleBooksMetadataProvider> log) : IMetadataProvider
{

    public string Name => "googlebooks";

    public bool Supports(MediaKind kind) => kind == MediaKind.Book;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind)) return [];
        var url = $"volumes?q={Uri.EscapeDataString(query)}&maxResults=20";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            if (!json.TryGetProperty("items", out var items)) return [];

            var results = new List<MetadataSearchResult>();
            foreach (var item in items.EnumerateArray())
            {
                // Each item is mapped independently so one bad item doesn't kill the whole list.
                var mapped = TryMap(item);
                if (mapped is not null) results.Add(mapped);
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "GoogleBooks search failed for {Query}", query);
            return [];
        }
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind)) return null;
        var url = $"volumes/{Uri.EscapeDataString(providerId)}";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            return TryMap(json);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "GoogleBooks fetch failed for {Id}", providerId);
            return null;
        }
    }

    private static MetadataSearchResult? TryMap(JsonElement item)
    {
        try
        {
            // Use TryGetProperty everywhere so a missing field doesn't crash the whole list.
            if (!item.TryGetProperty("id", out var idProp)) return null;
            var id = idProp.GetString() ?? "";
            if (string.IsNullOrEmpty(id)) return null;

            if (!item.TryGetProperty("volumeInfo", out var info)) return null;

            var title = info.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
            var subtitle = info.TryGetProperty("subtitle", out var s) ? s.GetString() : null;
            var fullTitle = subtitle is null ? title : $"{title}: {subtitle}";

            var overview = info.TryGetProperty("description", out var d) ? d.GetString() : null;

            DateOnly? releaseDate = null;
            if (info.TryGetProperty("publishedDate", out var pd))
            {
                var raw = pd.GetString();
                if (!string.IsNullOrEmpty(raw))
                {
                    if (DateOnly.TryParse(raw, out var d1))
                        releaseDate = d1;
                    else if (raw.Length >= 4 && int.TryParse(raw[..4], out var yr))
                        releaseDate = new DateOnly(yr, 1, 1);
                }
            }

            string? thumb = null;
            if (info.TryGetProperty("imageLinks", out var il) && il.TryGetProperty("thumbnail", out var tn))
                thumb = tn.GetString();

            double? rating = null;
            if (info.TryGetProperty("averageRating", out var ar) && ar.ValueKind == JsonValueKind.Number)
                rating = ar.GetDouble() * 2; // Google rates out of 5, normalise to 10

            var categories = info.TryGetProperty("categories", out var cats)
                ? cats.EnumerateArray()
                      .Select(c => c.GetString() ?? "")
                      .Where(c => !string.IsNullOrEmpty(c))
                      .ToList()
                : (IReadOnlyList<string>)[];

            return new MetadataSearchResult(
                ProviderId:    id,
                Title:         fullTitle,
                OriginalTitle: null,
                Overview:      overview,
                ReleaseDate:   releaseDate,
                PosterUrl:     thumb,
                BackdropUrl:   null,
                Rating:        rating,
                Genres:        categories,
                ProviderIds:   new Dictionary<string, string> { ["googlebooks"] = id });
        }
        catch
        {
            // Swallow per-item failures so the rest of the list is still returned.
            return null;
        }
    }
}
