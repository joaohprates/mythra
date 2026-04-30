using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Constructs Vidsrc.to iframe embed URLs for movies and TV series.
/// No HTTP requests are made — the URL is built locally from the media item's
/// IMDB or TMDB identifier.
/// </summary>
public sealed class VidsrcProvider(IOptions<ExternalProvidersOptions> options)
    : IExternalVideoProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "Vidsrc";
    public int    Priority => 10;

    public bool Supports(MediaKind kind) =>
        _opts.VidsrcEnabled && kind is MediaKind.Video;

    public Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default)
    {
        if (!_opts.VidsrcEnabled || request.Kind is not MediaKind.Video)
            return Task.FromResult<ExternalStreamResult?>(null);

        // Vidsrc supports IMDB (tt…) and TMDB numeric IDs
        var id = request.ImdbId ?? request.TmdbId;
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult<ExternalStreamResult?>(null);

        var isSeries = request.Season.HasValue;
        var type     = isSeries ? "tv" : "movie";

        var url = isSeries
            ? $"{_opts.VidsrcBaseUrl}/{type}/{id}/{request.Season}/{request.Episode ?? 1}"
            : $"{_opts.VidsrcBaseUrl}/{type}/{id}";

        return Task.FromResult<ExternalStreamResult?>(new ExternalStreamResult(
            ProviderName: Name,
            StreamKind:   ExternalStreamKind.IframeEmbed,
            Url:          url,
            RefererUrl:   "https://vidsrc.to"));
    }
}
