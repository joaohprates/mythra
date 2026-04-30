using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Media;
using Mythra.Application.Mapping;
using Mythra.Domain.Common;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Audio;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Services.Media;

public sealed class MediaService(
    IMediaItemRepository media,
    IVideoRepository videos,
    IMangaRepository mangas,
    IBookRepository books,
    IAudioRepository audios,
    IGenreRepository genres) : IMediaService
{
    public async Task<Result<PagedResult<MediaItemDto>>> ListAsync(MediaQuery query, CancellationToken ct = default)
    {
        if (query.Ids is { Count: > 0 })
        {
            var byIds = await media.ByIdsAsync(query.Ids, ct);
            var dtos = byIds.Select(i => i.ToSummary()).ToList();
            return new PagedResult<MediaItemDto>(dtos, dtos.Count, 0, dtos.Count);
        }
        var items = await media.SearchAsync(query, ct);
        var total = await media.CountAsync(query, ct);
        return new PagedResult<MediaItemDto>(items.Select(i => i.ToSummary()).ToList(), total, query.Skip, query.Take);
    }

    public async Task<Result<MediaItemDto>> GetSummaryAsync(Guid id, CancellationToken ct = default)
    {
        var item = await media.GetByIdAsync(id, ct);
        return item is null ? Error.NotFound("MediaItem", id) : item.ToSummary();
    }

    public async Task<Result<object>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var item = await media.GetByIdWithDetailsAsync(id, ct);
        if (item is null) return Error.NotFound("MediaItem", id);
        object dto = item switch
        {
            VideoItem v => await LoadVideoDetailAsync(v, ct),
            MangaItem m => await LoadMangaDetailAsync(m, ct),
            BookItem b => await LoadBookDetailAsync(b, ct),
            AudioItem a => await LoadAudioDetailAsync(a, ct),
            _ => item.ToSummary(),
        };
        return dto;
    }

    public async Task<Result<IReadOnlyList<MediaItemDto>>> RecentlyAddedAsync(Guid? libraryId, int take, CancellationToken ct = default)
    {
        var items = await media.RecentlyAddedAsync(libraryId, take, ct);
        return Result<IReadOnlyList<MediaItemDto>>.Success(items.Select(i => i.ToSummary()).ToList());
    }

    public async Task<Result<IReadOnlyList<string>>> ListGenresAsync(MediaKind? kind, CancellationToken ct = default)
    {
        var list = await genres.ListAsync(kind, ct);
        return Result<IReadOnlyList<string>>.Success(list.Select(g => g.Name).Distinct().OrderBy(n => n).ToList());
    }

    public async Task<Result<IReadOnlyList<VideoItemDto>>> ListEpisodesAsync(Guid seriesId, CancellationToken ct = default)
    {
        var episodes = await videos.ListEpisodesAsync(seriesId, ct);
        return Result<IReadOnlyList<VideoItemDto>>.Success(
            episodes.Select(v => v.ToDetail()).ToList());
    }

    private async Task<VideoItemDto> LoadVideoDetailAsync(VideoItem v, CancellationToken ct)
    {
        var hydrated = await videos.GetByIdWithStreamsAsync(v.Id, ct) ?? v;
        return hydrated.ToDetail();
    }

    private async Task<MangaItemDto> LoadMangaDetailAsync(MangaItem m, CancellationToken ct)
    {
        var hydrated = await mangas.GetByIdWithChaptersAsync(m.Id, ct) ?? m;
        return hydrated.ToDetail();
    }

    private async Task<BookItemDto> LoadBookDetailAsync(BookItem b, CancellationToken ct)
    {
        var hydrated = await books.GetByIdWithChaptersAsync(b.Id, ct) ?? b;
        return hydrated.ToDetail();
    }

    private async Task<AudioItemDto> LoadAudioDetailAsync(AudioItem a, CancellationToken ct)
    {
        var hydrated = await audios.GetByIdWithChaptersAsync(a.Id, ct) ?? a;
        return hydrated.ToDetail();
    }
}
