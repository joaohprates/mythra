using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Statistics;
using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.Statistics;

public sealed class StatisticsService(
    IPlaybackProgressRepository playbacks,
    IReadingProgressRepository readings,
    IMediaItemRepository media) : IStatisticsService
{
    public async Task<Result<ProfileStatisticsDto>> GetProfileStatisticsAsync(
        Guid profileId, int weekCount = 12, CancellationToken ct = default)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-weekCount * 7);

        var allPlayback = await playbacks.GetAllForProfileAsync(profileId, since, ct);
        var allReading  = await readings.GetAllForProfileAsync(profileId, since, ct);

        // Fetch media metadata for consumed items
        var allMediaIds = allPlayback.Select(p => p.MediaItemId)
            .Union(allReading.Select(r => r.MediaItemId))
            .Distinct()
            .ToList();

        var mediaMap = allMediaIds.Count == 0
            ? new Dictionary<Guid, MediaItem>()
            : (await media.ByIdsAsync(allMediaIds, ct)).ToDictionary(m => m.Id);

        // Total watch/read time
        var totalWatchTime = allPlayback.Aggregate(TimeSpan.Zero,
            (acc, p) => acc + (p.Duration ?? TimeSpan.Zero) * Math.Clamp(p.PercentComplete / 100.0, 0, 1));

        // Approximate read time: 2 minutes per percent point
        var totalReadTime = allReading.Aggregate(TimeSpan.Zero,
            (acc, r) => acc + TimeSpan.FromMinutes(r.PercentComplete * 2));

        var totalWatched   = allPlayback.DistinctBy(p => p.MediaItemId).Count();
        var totalRead      = allReading.DistinctBy(r => r.MediaItemId).Count();
        var totalCompleted = allPlayback.Count(p => p.IsCompleted) + allReading.Count(r => r.IsCompleted);

        // Genre breakdown
        var genreFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in allMediaIds)
        {
            if (!mediaMap.TryGetValue(id, out var item)) continue;
            foreach (var g in item.Genres)
                genreFreq[g.Name] = genreFreq.GetValueOrDefault(g.Name) + 1;
        }
        var totalGenreHits = genreFreq.Values.Sum();
        var topGenres = genreFreq
            .OrderByDescending(kv => kv.Value)
            .Take(8)
            .Select(kv => new GenreStatDto(
                kv.Key,
                kv.Value,
                totalGenreHits > 0 ? Math.Round(kv.Value * 100.0 / totalGenreHits, 1) : 0))
            .ToList();

        // Weekly activity
        var weeklyActivity = BuildWeeklyActivity(allPlayback, weekCount);

        // Kind breakdown
        var kindCounts = allMediaIds
            .Where(id => mediaMap.ContainsKey(id))
            .GroupBy(id => mediaMap[id].Kind)
            .ToDictionary(g => g.Key, g => g.Count());
        var totalKindHits = kindCounts.Values.Sum();
        var kindBreakdown = kindCounts
            .OrderByDescending(kv => kv.Value)
            .Select(kv => new MediaKindBreakdownDto(
                kv.Key,
                kv.Value,
                totalKindHits > 0 ? Math.Round(kv.Value * 100.0 / totalKindHits, 1) : 0))
            .ToList();

        return new ProfileStatisticsDto(
            ProfileId: profileId,
            TotalWatchTime: totalWatchTime,
            TotalReadTime: totalReadTime,
            TotalItemsWatched: totalWatched,
            TotalItemsRead: totalRead,
            TotalItemsCompleted: totalCompleted,
            TopGenres: topGenres,
            WeeklyActivity: weeklyActivity,
            KindBreakdown: kindBreakdown,
            GeneratedAt: DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<WeeklyActivityDto> BuildWeeklyActivity(
        IReadOnlyList<Domain.Progress.PlaybackProgress> playback, int weekCount)
    {
        var result = new List<WeeklyActivityDto>(weekCount);
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var i = weekCount - 1; i >= 0; i--)
        {
            var weekStart = now.AddDays(-i * 7 - (int)now.DayOfWeek);
            var weekEnd   = weekStart.AddDays(7);

            var weekItems = playback
                .Where(p => DateOnly.FromDateTime(p.LastWatchedAt.UtcDateTime) >= weekStart
                         && DateOnly.FromDateTime(p.LastWatchedAt.UtcDateTime) < weekEnd)
                .ToList();

            var watchTime = weekItems.Aggregate(TimeSpan.Zero,
                (acc, p) => acc + (p.Duration ?? TimeSpan.Zero) * Math.Clamp(p.PercentComplete / 100.0, 0, 1));

            result.Add(new WeeklyActivityDto(weekStart, weekItems.Count, watchTime));
        }

        return result;
    }
}
