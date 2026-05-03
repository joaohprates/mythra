using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

public sealed class MusicBrainzMetadataProvider(
    HttpClient http,
    IOptions<MetadataOptions> opts,
    ILogger<MusicBrainzMetadataProvider> log) : IMetadataProvider
{
    private readonly MetadataOptions _opts = opts.Value;

    public string Name => "musicbrainz";

    public bool Supports(MediaKind kind) => kind == MediaKind.Audio;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind)) return [];
        var url = $"release/?query={Uri.EscapeDataString(query)}&fmt=json&limit=20";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            if (!json.TryGetProperty("releases", out var releases)) return [];

            var results = new List<MetadataSearchResult>();
            foreach (var r in releases.EnumerateArray())
            {
                var mapped = TryMap(r);
                if (mapped is not null) results.Add(mapped);
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "MusicBrainz search failed for {Query}", query);
            return [];
        }
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind)) return null;
        // Fetch release details including artist credits.
        var url = $"release/{Uri.EscapeDataString(providerId)}?fmt=json&inc=artist-credits+labels";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            return TryMap(json);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "MusicBrainz fetch failed for {Id}", providerId);
            return null;
        }
    }

    private static MetadataSearchResult? TryMap(JsonElement r)
    {
        try
        {
            if (!r.TryGetProperty("id", out var idProp)) return null;
            var id = idProp.GetString() ?? "";
            if (string.IsNullOrEmpty(id)) return null;

            var title = r.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";

            string? artist = null;
            if (r.TryGetProperty("artist-credit", out var ac) && ac.GetArrayLength() > 0
                && ac[0].TryGetProperty("name", out var an))
                artist = an.GetString();

            DateOnly? releaseDate = null;
            if (r.TryGetProperty("date", out var dt))
            {
                var raw = dt.GetString();
                if (!string.IsNullOrEmpty(raw))
                {
                    if (DateOnly.TryParse(raw, out var d)) releaseDate = d;
                    else if (raw.Length >= 4 && int.TryParse(raw[..4], out var yr))
                        releaseDate = new DateOnly(yr, 1, 1);
                }
            }

            return new MetadataSearchResult(
                ProviderId:    id,
                Title:         title,
                OriginalTitle: artist,
                Overview:      null,
                ReleaseDate:   releaseDate,
                PosterUrl:     null,
                BackdropUrl:   null,
                Rating:        null,
                Genres:        [],
                ProviderIds:   new Dictionary<string, string> { ["musicbrainz"] = id });
        }
        catch
        {
            return null;
        }
    }
}
