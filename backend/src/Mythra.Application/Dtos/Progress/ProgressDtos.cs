namespace Mythra.Application.Dtos.Progress;

public sealed record PlaybackProgressDto(
    Guid MediaItemId,
    TimeSpan Position,
    TimeSpan? Duration,
    bool IsCompleted,
    DateTimeOffset LastWatchedAt,
    double PercentComplete,
    double PlaybackSpeed);

public sealed record UpdatePlaybackRequest(TimeSpan Position, TimeSpan? Duration, double? PlaybackSpeed, int? AudioStreamIndex, int? SubtitleStreamIndex);

public sealed record ReadingProgressDto(
    Guid MediaItemId,
    Guid? CurrentChapterId,
    int? CurrentPage,
    int? TotalPages,
    string? CfiLocator,
    double PercentComplete,
    bool IsCompleted,
    DateTimeOffset LastReadAt);

public sealed record UpdateReadingRequest(double PercentComplete, int? CurrentPage, string? CfiLocator, Guid? CurrentChapterId);

public sealed record BookmarkDto(Guid Id, string? Label, string? Note, TimeSpan? Position, int? Page, string? CfiLocator, DateTimeOffset CreatedAt);

public sealed record CreateBookmarkRequest(string? Label, string? Note, TimeSpan? Position, int? Page, string? CfiLocator);

public sealed record HighlightDto(Guid Id, string Text, string? Note, string Color, string? CfiStart, string? CfiEnd, int? Page, DateTimeOffset CreatedAt);

public sealed record CreateHighlightRequest(string Text, string? Note, string Color, string? CfiStart, string? CfiEnd, int? Page);
