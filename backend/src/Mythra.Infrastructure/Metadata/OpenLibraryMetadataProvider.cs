using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

/// <summary>
/// Metadata provider backed by the Open Library API (openlibrary.org).
/// Free, no API key required. ProviderId is the Open Library work key (e.g. "OL45883W").
/// Stored in ProviderGoogleBooksId column for backwards compatibility (no new migration needed).
/// </summary>
public sealed class OpenLibraryMetadataProvider(
    HttpClient http,
    IMemoryCache cache,
    ILogger<OpenLibraryMetadataProvider> log) : IMetadataProvider
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public string Name => "openlibrary";
    public bool Supports(MediaKind kind) => kind == MediaKind.Book;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(query)) return [];

        var url = $"search.json?q={Uri.EscapeDataString(query)}&fields=key,title,author_name,first_publish_year,cover_i,subject&limit=20";
        if (year.HasValue) url += $"&first_publish_year={year}";

        try
        {
            var jsonStr = await GetJsonCachedAsync(url, ct);
            if (jsonStr is null) return [];
            var json = JsonSerializer.Deserialize<JsonElement>(jsonStr);
            if (!json.TryGetProperty("docs", out var docs)) return [];

            var results = new List<MetadataSearchResult>();
            foreach (var doc in docs.EnumerateArray())
            {
                var mapped = TryMapDoc(doc);
                if (mapped is not null) results.Add(mapped);
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[OpenLibrary] Search failed for '{Query}'", query);
            return [];
        }
    }

    private async Task<string?> GetJsonCachedAsync(string url, CancellationToken ct)
    {
        var key = $"openlibrary:{url}";
        if (cache.TryGetValue(key, out string? json) && json is not null) return json;
        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;
        json = await resp.Content.ReadAsStringAsync(ct);
        cache.Set(key, json, CacheTtl);
        return json;
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(
        string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(providerId)) return null;

        // Accept both "OL45883W" and "/works/OL45883W"
        var workId = providerId.TrimStart('/');
        if (!workId.StartsWith("works/", StringComparison.OrdinalIgnoreCase))
            workId = $"works/{workId}";

        try
        {
            var jsonStr = await GetJsonCachedAsync($"{workId}.json", ct);
            if (jsonStr is null) return null;
            var json = JsonSerializer.Deserialize<JsonElement>(jsonStr);
            return TryMapWork(json, workId.Split('/').Last());
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[OpenLibrary] Work fetch failed for {Id}", providerId);
            return null;
        }
    }

    private static MetadataSearchResult? TryMapDoc(JsonElement doc)
    {
        try
        {
            if (!doc.TryGetProperty("key", out var keyProp)) return null;
            var key = keyProp.GetString();
            if (string.IsNullOrEmpty(key)) return null;

            var id = key.Split('/').Last();
            if (string.IsNullOrEmpty(id)) return null;

            var title = doc.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";

            string? author = null;
            if (doc.TryGetProperty("author_name", out var authors) && authors.GetArrayLength() > 0)
                author = authors[0].GetString();

            DateOnly? releaseDate = null;
            if (doc.TryGetProperty("first_publish_year", out var fpy) && fpy.ValueKind == JsonValueKind.Number
                && fpy.GetInt32() is > 0 and < 2100)
                releaseDate = new DateOnly(fpy.GetInt32(), 1, 1);

            string? poster = null;
            if (doc.TryGetProperty("cover_i", out var coverId) && coverId.ValueKind == JsonValueKind.Number)
                poster = $"https://covers.openlibrary.org/b/id/{coverId.GetInt64()}-M.jpg";

            var genres = new List<string>();
            if (doc.TryGetProperty("subject", out var subjects))
                foreach (var s in subjects.EnumerateArray().Take(5))
                {
                    var subj = s.GetString();
                    if (!string.IsNullOrEmpty(subj)) genres.Add(subj);
                }

            return new MetadataSearchResult(
                ProviderId:    id,
                Title:         title,
                OriginalTitle: author is not null ? $"by {author}" : null,
                Overview:      null,
                ReleaseDate:   releaseDate,
                PosterUrl:     poster,
                BackdropUrl:   null,
                Rating:        null,
                Genres:        genres,
                ProviderIds:   new Dictionary<string, string> { ["openlibrary"] = id, ["google"] = id });
        }
        catch
        {
            return null;
        }
    }

    private static MetadataSearchResult? TryMapWork(JsonElement work, string id)
    {
        try
        {
            var title = work.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";

            string? overview = null;
            if (work.TryGetProperty("description", out var desc))
            {
                overview = desc.ValueKind == JsonValueKind.String
                    ? desc.GetString()
                    : desc.TryGetProperty("value", out var v) ? v.GetString() : null;
            }

            var genres = new List<string>();
            if (work.TryGetProperty("subjects", out var subj))
                foreach (var s in subj.EnumerateArray().Take(5))
                {
                    var subjectStr = s.GetString();
                    if (!string.IsNullOrEmpty(subjectStr)) genres.Add(subjectStr);
                }

            string? poster = null;
            if (work.TryGetProperty("covers", out var covers) && covers.GetArrayLength() > 0
                && covers[0].ValueKind == JsonValueKind.Number)
                poster = $"https://covers.openlibrary.org/b/id/{covers[0].GetInt64()}-M.jpg";

            return new MetadataSearchResult(
                ProviderId:    id,
                Title:         title,
                OriginalTitle: null,
                Overview:      overview,
                ReleaseDate:   null,
                PosterUrl:     poster,
                BackdropUrl:   null,
                Rating:        null,
                Genres:        genres,
                ProviderIds:   new Dictionary<string, string> { ["openlibrary"] = id, ["google"] = id });
        }
        catch
        {
            return null;
        }
    }
}
