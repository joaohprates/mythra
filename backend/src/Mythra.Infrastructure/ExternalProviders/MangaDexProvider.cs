using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Searches MangaDex for manga titles and returns online reader URLs.
/// A <see cref="SemaphoreSlim"/> enforces the public rate limit of 5 req/s.
/// </summary>
public sealed class MangaDexProvider(
    HttpClient                         http,
    IOptions<ExternalProvidersOptions> options,
    ILogger<MangaDexProvider>          logger) : IExternalBookProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    // Shared within this singleton instance; MangaDex allows 5 concurrent req/s
    private readonly SemaphoreSlim _throttle = new(initialCount: 5, maxCount: 5);

    public string Name     => "MangaDex";
    public int    Priority => 10;

    public bool Supports(MediaKind kind) =>
        _opts.MangaDexEnabled && kind is MediaKind.Manga;

    public async Task<IReadOnlyList<ExternalBookResult>> GetLinksAsync(
        ExternalBookRequest request,
        CancellationToken   ct = default)
    {
        if (!_opts.MangaDexEnabled || request.Kind is not MediaKind.Manga)
            return [];

        await _throttle.WaitAsync(ct);
        try
        {
            var query    = Uri.EscapeDataString(request.Title);
            var response = await http.GetFromJsonAsync<MangaDexSearchResponse>(
                $"{_opts.MangaDexBaseUrl}/manga?title={query}&limit=5&includes[]=cover_art",
                ct);

            if (response?.Data is null) return [];

            return response.Data.Select(manga =>
            {
                var coverRel  = manga.Relationships?.FirstOrDefault(r => r.Type == "cover_art");
                var coverFile = coverRel?.Attributes?.FileName;
                var coverUrl  = coverFile is not null
                    ? $"https://uploads.mangadex.org/covers/{manga.Id}/{coverFile}.256.jpg"
                    : null;

                return new ExternalBookResult(
                    ProviderName: Name,
                    Format:       ExternalBookFormat.WebReader,
                    Url:          $"https://mangadex.org/title/{manga.Id}",
                    CoverUrl:     coverUrl,
                    Language:     "en");
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[MangaDex] Search failed for '{Title}'", request.Title);
            return [];
        }
        finally
        {
            _throttle.Release();
        }
    }

    // ── MangaDex JSON models ──────────────────────────────────────────────────

    private sealed record MangaDexSearchResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<MangaDexManga>? Data);

    private sealed record MangaDexManga(
        [property: JsonPropertyName("id")]            string Id,
        [property: JsonPropertyName("attributes")]    MangaAttributes? Attributes,
        [property: JsonPropertyName("relationships")] IReadOnlyList<MangaRelationship>? Relationships);

    private sealed record MangaAttributes(
        [property: JsonPropertyName("title")] IReadOnlyDictionary<string, string>? Title);

    private sealed record MangaRelationship(
        [property: JsonPropertyName("type")]       string Type,
        [property: JsonPropertyName("attributes")] CoverAttributes? Attributes);

    private sealed record CoverAttributes(
        [property: JsonPropertyName("fileName")] string? FileName);
}
