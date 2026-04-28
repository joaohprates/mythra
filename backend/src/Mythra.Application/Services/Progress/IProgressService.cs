using Mythra.Application.Dtos.Progress;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Progress;

public interface IProgressService
{
    Task<Result<PlaybackProgressDto?>> GetPlaybackAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<Result<PlaybackProgressDto>> UpdatePlaybackAsync(Guid profileId, Guid mediaItemId, UpdatePlaybackRequest req, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PlaybackProgressDto>>> ContinueWatchingAsync(Guid profileId, int take, CancellationToken ct = default);

    Task<Result<ReadingProgressDto?>> GetReadingAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<Result<ReadingProgressDto>> UpdateReadingAsync(Guid profileId, Guid mediaItemId, UpdateReadingRequest req, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ReadingProgressDto>>> ContinueReadingAsync(Guid profileId, int take, CancellationToken ct = default);

    Task<Result<IReadOnlyList<BookmarkDto>>> ListBookmarksAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<Result<BookmarkDto>> AddBookmarkAsync(Guid profileId, Guid mediaItemId, CreateBookmarkRequest req, CancellationToken ct = default);
    Task<Result> RemoveBookmarkAsync(Guid profileId, Guid bookmarkId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<HighlightDto>>> ListHighlightsAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<Result<HighlightDto>> AddHighlightAsync(Guid profileId, Guid mediaItemId, CreateHighlightRequest req, CancellationToken ct = default);
    Task<Result> RemoveHighlightAsync(Guid profileId, Guid highlightId, CancellationToken ct = default);
}
