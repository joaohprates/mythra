using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Videasy player — multilingual iframe embed provider.
/// Supports TMDB IDs; has Portuguese ("pt") audio/subtitle options.
/// Movie URL: https://player.videasy.net/movie/{tmdbId}
/// TV URL:    https://player.videasy.net/tv/{tmdbId}/{season}/{episode}
/// </summary>
public sealed class VideasyProvider(IOptions<ExternalProvidersOptions> options) : IExternalVideoProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "Videasy";
    public int    Priority => 5; // Higher priority than Vidsrc (lower number = tried first)

    public bool Supports(MediaKind kind) =>
        _opts.VideasyEnabled && kind is MediaKind.Video;

    public Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default)
    {
        if (!_opts.VideasyEnabled || string.IsNullOrWhiteSpace(request.TmdbId))
            return Task.FromResult<ExternalStreamResult?>(null);

        var isSeries = request.Season.HasValue;
        var type     = isSeries ? "tv" : "movie";

        var url = isSeries
            ? $"{_opts.VideasyBaseUrl}/{type}/{request.TmdbId}/{request.Season}/{request.Episode ?? 1}"
            : $"{_opts.VideasyBaseUrl}/{type}/{request.TmdbId}";

        return Task.FromResult<ExternalStreamResult?>(new ExternalStreamResult(
            ProviderName: Name,
            StreamKind:   ExternalStreamKind.IframeEmbed,
            Url:          url,
            RefererUrl:   "https://videasy.net"));
    }
}
