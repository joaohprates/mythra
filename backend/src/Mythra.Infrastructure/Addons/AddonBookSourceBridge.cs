using Microsoft.Extensions.Logging;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Adapts an <see cref="IBookSourceAddon"/> from the addon SDK into the application's
/// <see cref="IExternalBookProvider"/> contract so it can participate in the existing
/// fallback chain inside <c>ExternalProviderService</c>.
/// </summary>
internal sealed class AddonBookSourceBridge(IBookSourceAddon addon, ILogger logger)
    : IExternalBookProvider
{
    public string Name => addon.Id;

    // IExternalBookProvider treats lower = tried first; addons advertise higher = tried first.
    public int Priority => -addon.Priority;

    public bool Supports(MediaKind kind) => addon.Supports(MapToAddonKind(kind));

    public async Task<IReadOnlyList<ExternalBookResult>> GetLinksAsync(
        ExternalBookRequest request,
        CancellationToken   ct = default)
    {
        try
        {
            var addonRequest = new AddonBookRequest(
                MediaItemId:   request.MediaItemId,
                Title:         request.Title,
                Kind:          MapToAddonKind(request.Kind),
                MangaDexId:    request.MangaDexId,
                AniListId:     null,
                Author:        request.Author,
                GutenbergId:   request.GutenbergId,
                GoogleBooksId: request.GoogleBooksId,
                Isbn:          request.Isbn);

            var results = await addon.GetLinksAsync(addonRequest, ct);

            return results.Select(r => new ExternalBookResult(
                ProviderName: addon.Id,
                Format:       MapFormat(r.Format),
                Url:          r.Url,
                Language:     r.Language)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Book addon {AddonId} threw during GetLinksAsync.", addon.Id);
            return [];
        }
    }

    private static AddonMediaKind MapToAddonKind(MediaKind kind) => kind switch
    {
        MediaKind.Manga => AddonMediaKind.Manga,
        MediaKind.Book  => AddonMediaKind.Book,
        _               => AddonMediaKind.Book,
    };

    private static ExternalBookFormat MapFormat(AddonBookFormat format) => format switch
    {
        AddonBookFormat.Epub      => ExternalBookFormat.Epub,
        AddonBookFormat.Pdf       => ExternalBookFormat.Pdf,
        AddonBookFormat.PlainText => ExternalBookFormat.PlainText,
        AddonBookFormat.Mp3       => ExternalBookFormat.Mp3,
        AddonBookFormat.WebReader => ExternalBookFormat.WebReader,
        _                         => ExternalBookFormat.WebReader,
    };
}
