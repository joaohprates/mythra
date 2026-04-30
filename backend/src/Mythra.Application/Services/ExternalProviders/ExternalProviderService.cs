using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.ExternalProviders;

public sealed class ExternalProviderService(
    IEnumerable<IExternalVideoProvider> videoProviders,
    IEnumerable<IExternalBookProvider>  bookProviders,
    IMediaItemRepository                mediaItems) : IExternalProviderService
{
    // Ordered once at construction time — avoids repeated LINQ sorting per request
    private readonly IReadOnlyList<IExternalVideoProvider> _videoProviders =
        videoProviders.OrderBy(p => p.Priority).ToList();

    private readonly IReadOnlyList<IExternalBookProvider> _bookProviders =
        bookProviders.OrderBy(p => p.Priority).ToList();

    /// <inheritdoc/>
    public async Task<Result<ExternalVideoStreamDto>> GetVideoStreamAsync(
        Guid              mediaItemId,
        int?              season  = null,
        int?              episode = null,
        CancellationToken ct      = default)
    {
        var item = await mediaItems.GetByIdWithDetailsAsync(mediaItemId, ct);
        if (item is null)
            return Error.NotFound(nameof(MediaItem), mediaItemId);

        var request = new ExternalStreamRequest(
            MediaItemId: mediaItemId,
            Title:       item.Title,
            Kind:        item.Kind,
            ImdbId:      item.ProviderImdbId,
            TmdbId:      item.ProviderTmdbId,
            AniListId:   item.ProviderAnilistId,
            Season:      season,
            Episode:     episode);

        foreach (var provider in _videoProviders)
        {
            if (!provider.Supports(item.Kind)) continue;

            var result = await provider.GetStreamAsync(request, ct);
            if (result is not null)
            {
                return new ExternalVideoStreamDto(
                    ProviderName: result.ProviderName,
                    StreamKind:   result.StreamKind.ToString(),
                    Url:          result.Url,
                    RefererUrl:   result.RefererUrl,
                    Headers:      result.Headers);
            }
        }

        return Error.NotFound("ExternalStream", mediaItemId);
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ExternalBookLinkDto>>> GetBookLinksAsync(
        Guid              mediaItemId,
        CancellationToken ct = default)
    {
        var item = await mediaItems.GetByIdWithDetailsAsync(mediaItemId, ct);
        if (item is null)
            return Error.NotFound(nameof(MediaItem), mediaItemId);

        if (item.Kind is not (MediaKind.Book or MediaKind.Audio or MediaKind.Manga))
            return Error.Validation("Item must be of kind Book, Audio, or Manga.");

        var request = new ExternalBookRequest(
            MediaItemId:   mediaItemId,
            Title:         item.Title,
            Kind:          item.Kind,
            GoogleBooksId: item.ProviderGoogleBooksId);

        var all = new List<ExternalBookLinkDto>();
        foreach (var provider in _bookProviders)
        {
            if (!provider.Supports(item.Kind)) continue;

            var links = await provider.GetLinksAsync(request, ct);
            all.AddRange(links.Select(l => new ExternalBookLinkDto(
                ProviderName: l.ProviderName,
                Format:       l.Format.ToString(),
                Url:          l.Url,
                CoverUrl:     l.CoverUrl,
                Language:     l.Language,
                Authors:      l.Authors)));
        }

        return Result<IReadOnlyList<ExternalBookLinkDto>>.Success(all);
    }
}
