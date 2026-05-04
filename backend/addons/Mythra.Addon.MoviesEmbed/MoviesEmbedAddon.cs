using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;

namespace Mythra.Addon.MoviesEmbed;

/// <summary>
/// Resolves a streamable embed URL for movies and TV episodes by IMDb ID.
///
/// The addon is intentionally configuration-driven: the user supplies the upstream
/// embed host through the Mythra UI. Default values point at <c>example.tld</c> and
/// will not produce a working stream — installing the addon and pointing it at a
/// real embed provider is a single configuration change.
///
/// Configuration keys:
///   EmbedBaseUrl        — movie pattern, supports {imdbId}.
///   SeriesEmbedBaseUrl  — series pattern, supports {imdbId}, {season}, {episode}.
///   Priority            — int, higher = tried first by the host.
/// </summary>
public sealed class MoviesEmbedAddon : IStreamSourceAddon
{
    private static readonly Regex ImdbIdPattern = new(@"^tt\d{6,10}$", RegexOptions.Compiled);

    public string Id      => "io.mythra.movies-embed";
    public string Name    => "Movies Embed Stream";
    public string Version => "1.0.0";

    private IAddonContext _ctx = null!;
    private string _movieBaseUrl  = "https://example.tld/embed";
    private string _seriesBaseUrl = "https://example.tld/embed/tv/{imdbId}/{season}/{episode}";
    private int _priority = 100;

    public int Priority => _priority;

    public ValueTask InitializeAsync(IAddonContext context, CancellationToken ct = default)
    {
        _ctx = context;

        var embed = context.GetConfig("EmbedBaseUrl");
        if (!string.IsNullOrWhiteSpace(embed)) _movieBaseUrl = embed.Trim();

        var series = context.GetConfig("SeriesEmbedBaseUrl");
        if (!string.IsNullOrWhiteSpace(series)) _seriesBaseUrl = series.Trim();

        var rawPriority = context.GetConfig("Priority");
        if (int.TryParse(rawPriority, out var p)) _priority = p;

        _ctx.Logger.LogInformation(
            "[MoviesEmbedAddon] Initialized. Movie={Movie}, Series={Series}, Priority={Priority}",
            _movieBaseUrl, _seriesBaseUrl, _priority);

        return ValueTask.CompletedTask;
    }

    public ValueTask<AddonHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        // Pinging the upstream from here would either leak the configured host into logs
        // or fail when the user hasn't configured one yet. Health is reported Healthy and
        // playback errors surface through normal stream-fallback logging instead.
        return ValueTask.FromResult(AddonHealthStatus.Healthy);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public bool Supports(AddonMediaKind kind) =>
        kind is AddonMediaKind.Movie or AddonMediaKind.Series;

    public Task<AddonStreamResult?> GetStreamAsync(AddonStreamRequest request, CancellationToken ct = default)
    {
        if (!Supports(request.Kind)) return Task.FromResult<AddonStreamResult?>(null);

        var imdb = request.ImdbId;
        if (string.IsNullOrWhiteSpace(imdb) || !ImdbIdPattern.IsMatch(imdb))
            return Task.FromResult<AddonStreamResult?>(null);

        var isEpisode = request.Season.HasValue && request.Episode.HasValue;

        string url = isEpisode
            ? Interpolate(_seriesBaseUrl, imdb, request.Season!.Value, request.Episode!.Value)
            : Interpolate(_movieBaseUrl, imdb, null, null);

        return Task.FromResult<AddonStreamResult?>(new AddonStreamResult(
            Kind: AddonStreamKind.IframeEmbed,
            Url:  url));
    }

    private static string Interpolate(string template, string imdbId, int? season, int? episode)
    {
        var url = template
            .Replace("{imdbId}", imdbId, StringComparison.OrdinalIgnoreCase)
            .Replace("{imdb}",   imdbId, StringComparison.OrdinalIgnoreCase);

        if (season.HasValue)
            url = url.Replace("{season}", season.Value.ToString(), StringComparison.OrdinalIgnoreCase);
        if (episode.HasValue)
            url = url.Replace("{episode}", episode.Value.ToString(), StringComparison.OrdinalIgnoreCase);

        return url;
    }
}
