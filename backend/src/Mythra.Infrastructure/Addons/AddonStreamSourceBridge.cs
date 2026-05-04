using Microsoft.Extensions.Logging;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Adapts an <see cref="IStreamSourceAddon"/> from the addon SDK into the application's
/// <see cref="IExternalVideoProvider"/> contract so it can participate in the existing
/// fallback chain inside <c>ExternalProviderService</c>.
/// </summary>
internal sealed class AddonStreamSourceBridge(IStreamSourceAddon addon, ILogger logger)
    : IExternalVideoProvider
{
    public string Name => addon.Id;

    // IExternalVideoProvider treats lower = tried first; addons advertise higher = tried first.
    // Negate so a higher addon priority comes earlier in the orchestrator's ordering.
    public int Priority => -addon.Priority;

    public bool Supports(MediaKind kind) => addon.Supports(MapKind(kind));

    public async Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default)
    {
        try
        {
            var addonRequest = new AddonStreamRequest(
                MediaTitle: request.Title,
                ImdbId:     request.ImdbId,
                TmdbId:     request.TmdbId,
                Season:     request.Season,
                Episode:    request.Episode,
                Kind:       MapKind(request.Kind, request.IsSeries, request.IsAnime));

            var result = await addon.GetStreamAsync(addonRequest, ct);
            if (result is null) return null;

            return new ExternalStreamResult(
                ProviderName: addon.Id,
                StreamKind:   MapStreamKind(result.Kind),
                Url:          result.Url,
                RefererUrl:   null,
                Headers:      result.Headers,
                ExpiresInSeconds: null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stream addon {AddonId} threw during GetStreamAsync.", addon.Id);
            return null;
        }
    }

    private static AddonMediaKind MapKind(MediaKind kind, bool isSeries = false, bool isAnime = false)
    {
        if (kind == MediaKind.Manga) return AddonMediaKind.Manga;
        if (kind == MediaKind.Book)  return AddonMediaKind.Book;
        // Video — refine by IsSeries / IsAnime
        if (isAnime) return isSeries ? AddonMediaKind.Series : AddonMediaKind.Movie;
        return isSeries ? AddonMediaKind.Series : AddonMediaKind.Movie;
    }

    private static ExternalStreamKind MapStreamKind(AddonStreamKind k) => k switch
    {
        AddonStreamKind.IframeEmbed => ExternalStreamKind.IframeEmbed,
        AddonStreamKind.HlsManifest => ExternalStreamKind.HlsManifest,
        AddonStreamKind.DirectMp4   => ExternalStreamKind.DirectMp4,
        _                           => ExternalStreamKind.DirectMp4,
    };
}
