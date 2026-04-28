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
        var url = $"/release/?query={Uri.EscapeDataString(query)}&fmt=json&limit=20";
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(url, ct);
            if (!json.TryGetProperty("releases", out var releases)) return [];
            return releases.EnumerateArray().Select(Map).ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "MusicBrainz search failed for {Query}", query);
            return [];
        }
    }

    public Task<MetadataSearchResult?> GetByIdAsync(string providerId, MediaKind kind, CancellationToken ct = default) =>
        Task.FromResult<MetadataSearchResult?>(null);

    private static MetadataSearchResult Map(JsonElement r)
    {
        var id = r.GetProperty("id").GetString() ?? "";
        var title = r.TryGetProperty("title", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
        var artist = r.TryGetProperty("artist-credit", out var ac) && ac.GetArrayLength() > 0
            ? ac[0].GetProperty("name").GetString() : null;
        var date = r.TryGetProperty("date", out var dt) ? dt.GetString() : null;
        DateOnly? releaseDate = !string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var d) ? d : null;
        return new MetadataSearchResult(
            ProviderId: id,
            Title: title,
            OriginalTitle: artist,
            Overview: null,
            ReleaseDate: releaseDate,
            PosterUrl: null,
            BackdropUrl: null,
            Rating: null,
            Genres: [],
            ProviderIds: new Dictionary<string, string> { ["musicbrainz"] = id });
    }
}
