using Mythra.Domain.Media;

namespace Mythra.Application.Dtos.Statistics;

public sealed record ProfileStatisticsDto(
    Guid ProfileId,
    TimeSpan TotalWatchTime,
    TimeSpan TotalReadTime,
    int TotalItemsWatched,
    int TotalItemsRead,
    int TotalItemsCompleted,
    IReadOnlyList<GenreStatDto> TopGenres,
    IReadOnlyList<WeeklyActivityDto> WeeklyActivity,
    IReadOnlyList<MediaKindBreakdownDto> KindBreakdown,
    DateTimeOffset GeneratedAt);

public sealed record GenreStatDto(string Genre, int Count, double Percentage);

public sealed record WeeklyActivityDto(DateOnly Week, int ItemsWatched, TimeSpan WatchTime);

public sealed record MediaKindBreakdownDto(MediaKind Kind, int Count, double Percentage);
