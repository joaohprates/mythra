using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Fetches HLS stream URLs via the Consumet API (GogoAnime provider).
/// Works best for anime; enable via <c>ExternalProviders:ConsumetEnabled = true</c>
/// and point <c>ConsumetBaseUrl</c> at your self-hosted Consumet instance.
/// </summary>
public sealed class ConsumetProvider(
    HttpClient                         http,
    IOptions<ExternalProvidersOptions> options,
    ILogger<ConsumetProvider>          logger) : IExternalVideoProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "Consumet";
    public int    Priority => 20;

    public bool Supports(MediaKind kind) =>
        _opts.ConsumetEnabled && kind is MediaKind.Video;

    public async Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default)
    {
        if (!_opts.ConsumetEnabled || request.Kind is not MediaKind.Video)
            return null;

        try
        {
            // 1. Search for the title on GogoAnime
            var title  = Uri.EscapeDataString(request.Title);
            var search = await http.GetFromJsonAsync<ConsumetSearchResponse>(
                $"{_opts.ConsumetBaseUrl}/anime/gogoanime/{title}", ct);

            var match = search?.Results?.FirstOrDefault();
            if (match is null) return null;

            // 2. Get info to find the episode
            var info = await http.GetFromJsonAsync<ConsumetInfoResponse>(
                $"{_opts.ConsumetBaseUrl}/anime/gogoanime/info/{Uri.EscapeDataString(match.Id)}", ct);

            var epNumber = (request.Episode ?? 1).ToString();
            var episode  = info?.Episodes?.FirstOrDefault(e =>
                string.Equals(e.Number, epNumber, StringComparison.OrdinalIgnoreCase));

            if (episode is null) return null;

            // 3. Get streaming sources for the episode
            var watch = await http.GetFromJsonAsync<ConsumetWatchResponse>(
                $"{_opts.ConsumetBaseUrl}/anime/gogoanime/watch/{Uri.EscapeDataString(episode.Id)}", ct);

            var hls = watch?.Sources?.FirstOrDefault(s =>
                string.Equals(s.Type, "m3u8", StringComparison.OrdinalIgnoreCase) ||
                s.Url.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase));

            if (hls is null) return null;

            var referer = watch!.Headers?.GetValueOrDefault("Referer");
            return new ExternalStreamResult(
                ProviderName: Name,
                StreamKind:   ExternalStreamKind.HlsManifest,
                Url:          hls.Url,
                RefererUrl:   referer);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Consumet] Failed to get stream for '{Title}'", request.Title);
            return null;
        }
    }

    // ── Consumet response models ───────────────────────────────────────────

    private sealed record ConsumetSearchResponse(IReadOnlyList<ConsumetResult>? Results);
    private sealed record ConsumetResult(string Id, string Title);
    private sealed record ConsumetInfoResponse(IReadOnlyList<ConsumetEpisode>? Episodes);
    private sealed record ConsumetEpisode(string Id, string Number);
    private sealed record ConsumetWatchResponse(
        IReadOnlyList<ConsumetSource>?              Sources,
        IReadOnlyDictionary<string, string>?        Headers);
    private sealed record ConsumetSource(string Url, string Type);
}
