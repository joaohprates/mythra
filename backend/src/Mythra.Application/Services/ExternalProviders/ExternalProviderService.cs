using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Common;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Services.ExternalProviders;

public sealed class ExternalProviderService(
    IEnumerable<IExternalVideoProvider> videoProviders,
    IEnumerable<IExternalBookProvider>  bookProviders,
    IAddonStreamSourceRegistry          addonStreamRegistry,
    IAddonBookSourceRegistry            addonBookRegistry,
    IMediaItemRepository                mediaItems) : IExternalProviderService
{
    // Snapshot DI-registered providers once; addon-registered providers are merged on each call
    // so newly-loaded addons become visible immediately.
    private readonly IReadOnlyList<IExternalVideoProvider> _videoProviders =
        videoProviders.OrderBy(p => p.Priority).ToList();

    private readonly IReadOnlyList<IExternalBookProvider> _bookProviders =
        bookProviders.OrderBy(p => p.Priority).ToList();

    private IReadOnlyList<IExternalVideoProvider> AllVideoProviders() =>
        _videoProviders.Concat(addonStreamRegistry.GetAll()).OrderBy(p => p.Priority).ToList();

    private IReadOnlyList<IExternalBookProvider> AllBookProviders() =>
        _bookProviders.Concat(addonBookRegistry.GetAll()).OrderBy(p => p.Priority).ToList();

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

        // Derive isSeries / isAnime / season-episode from the typed VideoItem when possible.
        bool isSeries = false;
        bool isAnime  = false;
        int? effectiveSeason  = season;
        int? effectiveEpisode = episode;

        if (item is VideoItem video)
        {
            isAnime = video.IsAnime
                      || video.VideoKind is VideoKind.Anime or VideoKind.AnimeMovie;

            switch (video.VideoKind)
            {
                case VideoKind.Movie:
                case VideoKind.AnimeMovie:
                case VideoKind.Trailer:
                    isSeries = false;
                    effectiveSeason  = null;
                    effectiveEpisode = null;
                    break;
                case VideoKind.Episode:
                    isSeries = true;
                    effectiveSeason  ??= video.SeasonNumber;
                    effectiveEpisode ??= video.EpisodeNumber;
                    break;
                case VideoKind.Series:
                case VideoKind.Season:
                case VideoKind.Anime:
                case VideoKind.Special:
                    isSeries = true;
                    // Leave season/episode as supplied by caller (may be null → series page or first ep).
                    break;
                default:
                    isSeries = season.HasValue;
                    break;
            }
        }
        else
        {
            // Fallback for non-VideoItem callers
            isSeries = season.HasValue;
        }

        var request = new ExternalStreamRequest(
            MediaItemId: mediaItemId,
            Title:       item.Title,
            Kind:        item.Kind,
            ImdbId:      item.ProviderImdbId,
            TmdbId:      item.ProviderTmdbId,
            AniListId:   item.ProviderAnilistId,
            ArchiveOrgId:item.ProviderArchiveOrgId,
            Season:      effectiveSeason,
            Episode:     effectiveEpisode,
            IsSeries:    isSeries,
            IsAnime:     isAnime);

        foreach (var provider in AllVideoProviders())
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

        return Error.NotFound("ExternalStream", "No external source available. Install a streaming addon.");
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ExternalBookLinkDto>>> GetBookLinksAsync(
        Guid              mediaItemId,
        CancellationToken ct = default)
    {
        var item = await mediaItems.GetByIdWithDetailsAsync(mediaItemId, ct);
        if (item is null)
            return Error.NotFound(nameof(MediaItem), mediaItemId);

        if (item.Kind is not (MediaKind.Book or MediaKind.Manga))
            return Error.Validation("Item must be of kind Book or Manga.");

        string? author = item is BookItem book ? book.Author : null;
        string? isbn   = item is BookItem b2   ? b2.Isbn    : null;

        var request = new ExternalBookRequest(
            MediaItemId:   mediaItemId,
            Title:         item.Title,
            Kind:          item.Kind,
            Author:        author,
            GutenbergId:   item.ProviderGutenbergId,
            LibriVoxId:    item.ProviderLibriVoxId,
            GoogleBooksId: item.ProviderGoogleBooksId,
            MangaDexId:    item.ProviderMangaDexId,
            Isbn:          isbn);

        var all = new List<ExternalBookLinkDto>();
        foreach (var provider in AllBookProviders())
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
