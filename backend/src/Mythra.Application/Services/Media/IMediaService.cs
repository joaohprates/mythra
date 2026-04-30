using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Media;
using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.Media;

public interface IMediaService
{
    Task<Result<PagedResult<MediaItemDto>>> ListAsync(MediaQuery query, CancellationToken ct = default);
    Task<Result<MediaItemDto>> GetSummaryAsync(Guid id, CancellationToken ct = default);
    Task<Result<object>> GetDetailAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<MediaItemDto>>> RecentlyAddedAsync(Guid? libraryId, int take, CancellationToken ct = default);
    Task<Result<IReadOnlyList<string>>> ListGenresAsync(MediaKind? kind, CancellationToken ct = default);
    Task<Result<IReadOnlyList<VideoItemDto>>> ListEpisodesAsync(Guid seriesId, CancellationToken ct = default);
}
