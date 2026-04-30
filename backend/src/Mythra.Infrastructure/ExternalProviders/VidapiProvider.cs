using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Vidapi.ru — English-primary iframe embed provider, fallback when Videasy is unavailable.
/// Movie URL: https://vidapi.ru/embed/movie/{tmdbId}
/// TV URL:    https://vidapi.ru/embed/tv/{tmdbId}/{season}/{episode}
/// </summary>
public sealed class VidapiProvider(IOptions<ExternalProvidersOptions> options) : IExternalVideoProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "Vidapi";
    public int    Priority => 8; // Between Videasy (5) and Vidsrc (10)

    public bool Supports(MediaKind kind) =>
        _opts.VidapiEnabled && kind is MediaKind.Video;

    public Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default)
    {
        if (!_opts.VidapiEnabled || string.IsNullOrWhiteSpace(request.TmdbId))
            return Task.FromResult<ExternalStreamResult?>(null);

        var isSeries = request.Season.HasValue;
        var type     = isSeries ? "tv" : "movie";

        var url = isSeries
            ? $"{_opts.VidapiBaseUrl}/{type}/{request.TmdbId}/{request.Season}/{request.Episode ?? 1}"
            : $"{_opts.VidapiBaseUrl}/{type}/{request.TmdbId}";

        return Task.FromResult<ExternalStreamResult?>(new ExternalStreamResult(
            ProviderName: Name,
            StreamKind:   ExternalStreamKind.IframeEmbed,
            Url:          url,
            RefererUrl:   "https://vidapi.ru"));
    }
}
