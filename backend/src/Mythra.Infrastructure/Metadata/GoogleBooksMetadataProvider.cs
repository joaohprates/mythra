using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

public sealed class GoogleBooksMetadataProvider(
    HttpClient http,
    IOptions<MetadataOptions> opts,
    ILogger<GoogleBooksMetadataProvider> log) : IMetadataProvider
{
    private readonly MetadataOptions _opts = opts.Value;

    public string Name => "googlebooks";

    public bool Supports(MediaKind kind) => kind == MediaKind.Book;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind)) return [];
        var url = $"/volumes?q={Uri.EscapeDataString(query)}&maxResults=20";
        if (!string.IsNullOrEmpty(_opts.GoogleBooksApiKey)) url += $"&key={_opts.GoogleBooksApiKey}";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            if (!json.TryGetProperty("items", out var items)) return [];
            return items.EnumerateArray().Select(Map).ToList();
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
        var url = $"/volumes/{Uri.EscapeDataString(providerId)}";
        if (!string.IsNullOrEmpty(_opts.GoogleBooksApiKey)) url += $"?key={_opts.GoogleBooksApiKey}";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            return Map(json);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "GoogleBooks fetch failed for {Id}", providerId);
            return null;
        }
    }

    private static MetadataSearchResult Map(JsonElement item)
    {
        var id = item.GetProperty("id").GetString() ?? "";
        var info = item.GetProperty("volumeInfo");
        var title = info.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
        var subtitle = info.TryGetProperty("subtitle", out var s) ? s.GetString() : null;
        var fullTitle = subtitle is null ? title : $"{title}: {subtitle}";
        var overview = info.TryGetProperty("description", out var d) ? d.GetString() : null;
        var publishedDate = info.TryGetProperty("publishedDate", out var pd) ? pd.GetString() : null;
        DateOnly? releaseDate = null;
        if (!string.IsNullOrEmpty(publishedDate))
        {
            if (DateOnly.TryParse(publishedDate, out var d1)) releaseDate = d1;
            else if (int.TryParse(publishedDate.Length >= 4 ? publishedDate[..4] : publishedDate, out var year))
                releaseDate = new DateOnly(year, 1, 1);
        }
        var thumb = info.TryGetProperty("imageLinks", out var il) && il.TryGetProperty("thumbnail", out var tn) ? tn.GetString() : null;
        var rating = info.TryGetProperty("averageRating", out var ar) && ar.ValueKind == JsonValueKind.Number ? ar.GetDouble() * 2 : (double?)null;
        var categories = info.TryGetProperty("categories", out var cats)
            ? cats.EnumerateArray().Select(c => c.GetString() ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList()
            : [];

        return new MetadataSearchResult(
            ProviderId: id,
            Title: fullTitle,
            OriginalTitle: null,
            Overview: overview,
            ReleaseDate: releaseDate,
            PosterUrl: thumb,
            BackdropUrl: null,
            Rating: rating,
            Genres: categories,
            ProviderIds: new Dictionary<string, string> { ["googlebooks"] = id });
    }
}
