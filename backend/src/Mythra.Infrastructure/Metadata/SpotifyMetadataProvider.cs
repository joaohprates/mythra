using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

/// <summary>
/// Metadata provider backed by the Spotify Web API (albums + tracks).
/// Uses Client Credentials OAuth2 flow — no user login required.
/// Requires SpotifyClientId + SpotifyClientSecret in Metadata config section.
/// ProviderId is the Spotify album ID; stored in ProviderMusicbrainzId column.
/// </summary>
public sealed class SpotifyMetadataProvider(
    IHttpClientFactory httpFactory,
    IOptions<MetadataOptions> opts,
    IMemoryCache cache,
    ILogger<SpotifyMetadataProvider> log) : IMetadataProvider
{
    private readonly MetadataOptions _opts = opts.Value;
    private const string TokenCacheKey = "spotify_cc_token";
    private const string ApiBase = "https://api.spotify.com/v1/";
    private const string TokenUrl = "https://accounts.spotify.com/api/token";

    public string Name => "spotify";
    public bool Supports(MediaKind kind) => kind == MediaKind.Audio;

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(query)) return [];

        var token = await GetTokenAsync(ct);
        if (token is null)
        {
            log.LogDebug("[Spotify] Skipping search — no valid token (credentials not configured?)");
            return [];
        }

        using var http = CreateApiClient(token);
        var q = Uri.EscapeDataString(year.HasValue ? $"{query} year:{year}" : query);

        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>(
                $"search?q={q}&type=album&limit=20&market=US", JsonOpts, ct);

            if (!json.TryGetProperty("albums", out var albums) ||
                !albums.TryGetProperty("items", out var items))
                return [];

            var results = new List<MetadataSearchResult>();
            foreach (var item in items.EnumerateArray())
            {
                var mapped = TryMapAlbum(item);
                if (mapped is not null) results.Add(mapped);
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[Spotify] Search failed for '{Query}'", query);
            return [];
        }
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(
        string providerId, MediaKind kind, CancellationToken ct = default)
    {
        if (!Supports(kind) || string.IsNullOrWhiteSpace(providerId)) return null;

        var token = await GetTokenAsync(ct);
        if (token is null) return null;

        using var http = CreateApiClient(token);
        try
        {
            var json = await http.GetFromJsonAsync<JsonElement>($"albums/{providerId}", JsonOpts, ct);
            return TryMapAlbum(json);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[Spotify] Album fetch failed for {Id}", providerId);
            return null;
        }
    }

    // ── OAuth2 client credentials ─────────────────────────────────────────────

    private async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        if (cache.TryGetValue<string>(TokenCacheKey, out var cached) && cached is not null)
            return cached;

        if (string.IsNullOrWhiteSpace(_opts.SpotifyClientId) ||
            string.IsNullOrWhiteSpace(_opts.SpotifyClientSecret))
            return null;

        using var http = httpFactory.CreateClient("SpotifyAuth");
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_opts.SpotifyClientId}:{_opts.SpotifyClientSecret}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        try
        {
            var body = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
            ]);
            var resp = await http.PostAsync(TokenUrl, body, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(JsonOpts, ct);
            if (!json.TryGetProperty("access_token", out var tok)) return null;

            var token = tok.GetString();
            var expiresIn = json.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
            cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(expiresIn - 120));
            return token;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[Spotify] Token fetch failed");
            return null;
        }
    }

    private static HttpClient CreateApiClient(string token)
    {
        // Named client created without base address so we can set it here.
        // IHttpClientFactory is called from DI; a simple HttpClient is fine for one-shot calls.
        var http = new HttpClient { BaseAddress = new Uri(ApiBase) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return http;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static MetadataSearchResult? TryMapAlbum(JsonElement item)
    {
        try
        {
            if (!item.TryGetProperty("id", out var idProp)) return null;
            var id = idProp.GetString();
            if (string.IsNullOrEmpty(id)) return null;

            var albumName = item.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";

            string? artist = null;
            if (item.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0 &&
                artists[0].TryGetProperty("name", out var artistName))
                artist = artistName.GetString();

            DateOnly? releaseDate = null;
            if (item.TryGetProperty("release_date", out var rd))
            {
                var raw = rd.GetString() ?? "";
                if (DateOnly.TryParse(raw, out var d)) releaseDate = d;
                else if (raw.Length >= 4 && int.TryParse(raw[..4], out var yr))
                    releaseDate = new DateOnly(yr, 1, 1);
            }

            string? poster = null;
            if (item.TryGetProperty("images", out var images) && images.GetArrayLength() > 0 &&
                images[0].TryGetProperty("url", out var imgUrl))
                poster = imgUrl.GetString();

            var genres = new List<string>();
            if (item.TryGetProperty("genres", out var genreArr))
                foreach (var g in genreArr.EnumerateArray().Take(5))
                {
                    var gStr = g.GetString();
                    if (!string.IsNullOrEmpty(gStr)) genres.Add(gStr);
                }

            var title = artist is not null ? $"{artist} — {albumName}" : albumName;

            return new MetadataSearchResult(
                ProviderId:    id,
                Title:         title,
                OriginalTitle: albumName,
                Overview:      null,
                ReleaseDate:   releaseDate,
                PosterUrl:     poster,
                BackdropUrl:   null,
                Rating:        null,
                Genres:        genres,
                ProviderIds:   new Dictionary<string, string> { ["spotify"] = id, ["musicbrainz"] = id });
        }
        catch
        {
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
}
