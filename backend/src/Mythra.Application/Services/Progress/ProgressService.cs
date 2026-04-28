using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Progress;
using Mythra.Domain.Common;
using Mythra.Domain.Progress;

namespace Mythra.Application.Services.Progress;

public sealed class ProgressService(
    IPlaybackProgressRepository playbacks,
    IReadingProgressRepository readings,
    IBookmarkRepository bookmarks,
    IHighlightRepository highlights,
    IUnitOfWork uow) : IProgressService
{
    public async Task<Result<PlaybackProgressDto?>> GetPlaybackAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default)
    {
        var p = await playbacks.GetAsync(profileId, mediaItemId, ct);
        return Result<PlaybackProgressDto?>.Success(p is null ? null : ToDto(p));
    }

    public async Task<Result<PlaybackProgressDto>> UpdatePlaybackAsync(Guid profileId, Guid mediaItemId, UpdatePlaybackRequest req, CancellationToken ct = default)
    {
        var p = await playbacks.GetAsync(profileId, mediaItemId, ct);
        if (p is null)
        {
            p = new PlaybackProgress { ProfileId = profileId, MediaItemId = mediaItemId };
            await playbacks.AddAsync(p, ct);
        }
        p.UpdatePosition(req.Position, req.Duration);
        if (req.PlaybackSpeed.HasValue) p.PlaybackSpeed = Math.Clamp(req.PlaybackSpeed.Value, 0.25, 4.0);
        if (req.AudioStreamIndex.HasValue) p.PreferredAudioStreamIndex = req.AudioStreamIndex;
        if (req.SubtitleStreamIndex.HasValue) p.PreferredSubtitleStreamIndex = req.SubtitleStreamIndex;
        await uow.SaveChangesAsync(ct);
        return ToDto(p);
    }

    public async Task<Result<IReadOnlyList<PlaybackProgressDto>>> ContinueWatchingAsync(Guid profileId, int take, CancellationToken ct = default)
    {
        var list = await playbacks.ContinueWatchingAsync(profileId, take, ct);
        return Result<IReadOnlyList<PlaybackProgressDto>>.Success(list.Select(ToDto).ToList());
    }

    public async Task<Result<ReadingProgressDto?>> GetReadingAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default)
    {
        var r = await readings.GetAsync(profileId, mediaItemId, ct);
        return Result<ReadingProgressDto?>.Success(r is null ? null : ToDto(r));
    }

    public async Task<Result<ReadingProgressDto>> UpdateReadingAsync(Guid profileId, Guid mediaItemId, UpdateReadingRequest req, CancellationToken ct = default)
    {
        var r = await readings.GetAsync(profileId, mediaItemId, ct);
        if (r is null)
        {
            r = new ReadingProgress { ProfileId = profileId, MediaItemId = mediaItemId };
            await readings.AddAsync(r, ct);
        }
        r.UpdateProgress(req.PercentComplete, req.CurrentPage, req.CfiLocator);
        if (req.CurrentChapterId.HasValue) r.CurrentChapterId = req.CurrentChapterId;
        await uow.SaveChangesAsync(ct);
        return ToDto(r);
    }

    public async Task<Result<IReadOnlyList<ReadingProgressDto>>> ContinueReadingAsync(Guid profileId, int take, CancellationToken ct = default)
    {
        var list = await readings.ContinueReadingAsync(profileId, take, ct);
        return Result<IReadOnlyList<ReadingProgressDto>>.Success(list.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<BookmarkDto>>> ListBookmarksAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default)
    {
        var list = await bookmarks.ListAsync(profileId, mediaItemId, ct);
        return Result<IReadOnlyList<BookmarkDto>>.Success(list.Select(ToDto).ToList());
    }

    public async Task<Result<BookmarkDto>> AddBookmarkAsync(Guid profileId, Guid mediaItemId, CreateBookmarkRequest req, CancellationToken ct = default)
    {
        var bm = new Bookmark
        {
            ProfileId = profileId,
            MediaItemId = mediaItemId,
            Label = req.Label,
            Note = req.Note,
            Position = req.Position,
            Page = req.Page,
            CfiLocator = req.CfiLocator,
        };
        await bookmarks.AddAsync(bm, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(bm);
    }

    public async Task<Result> RemoveBookmarkAsync(Guid profileId, Guid bookmarkId, CancellationToken ct = default)
    {
        var bm = await bookmarks.GetByIdAsync(bookmarkId, ct);
        if (bm is null || bm.ProfileId != profileId) return Error.NotFound("Bookmark", bookmarkId);
        bookmarks.Remove(bm);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<HighlightDto>>> ListHighlightsAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default)
    {
        var list = await highlights.ListAsync(profileId, mediaItemId, ct);
        return Result<IReadOnlyList<HighlightDto>>.Success(list.Select(ToDto).ToList());
    }

    public async Task<Result<HighlightDto>> AddHighlightAsync(Guid profileId, Guid mediaItemId, CreateHighlightRequest req, CancellationToken ct = default)
    {
        var h = new Highlight
        {
            ProfileId = profileId,
            MediaItemId = mediaItemId,
            Text = req.Text,
            Note = req.Note,
            Color = req.Color,
            CfiStart = req.CfiStart,
            CfiEnd = req.CfiEnd,
            Page = req.Page,
        };
        await highlights.AddAsync(h, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(h);
    }

    public async Task<Result> RemoveHighlightAsync(Guid profileId, Guid highlightId, CancellationToken ct = default)
    {
        var h = await highlights.GetByIdAsync(highlightId, ct);
        if (h is null || h.ProfileId != profileId) return Error.NotFound("Highlight", highlightId);
        highlights.Remove(h);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static PlaybackProgressDto ToDto(PlaybackProgress p) =>
        new(p.MediaItemId, p.Position, p.Duration, p.IsCompleted, p.LastWatchedAt, p.PercentComplete, p.PlaybackSpeed);

    private static ReadingProgressDto ToDto(ReadingProgress r) =>
        new(r.MediaItemId, r.CurrentChapterId, r.CurrentPage, r.TotalPages, r.CfiLocator, r.PercentComplete, r.IsCompleted, r.LastReadAt);

    private static BookmarkDto ToDto(Bookmark b) =>
        new(b.Id, b.Label, b.Note, b.Position, b.Page, b.CfiLocator, b.CreatedAt);

    private static HighlightDto ToDto(Highlight h) =>
        new(h.Id, h.Text, h.Note, h.Color, h.CfiStart, h.CfiEnd, h.Page, h.CreatedAt);
}
