using FluentAssertions;
using Moq;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Services.Statistics;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Video;
using Mythra.Domain.Progress;

namespace Mythra.Application.Tests.Statistics;

public class StatisticsServiceTests
{
    private readonly Mock<IPlaybackProgressRepository> _playbacks = new();
    private readonly Mock<IReadingProgressRepository> _readings = new();
    private readonly Mock<IMediaItemRepository> _media = new();

    private StatisticsService Build() =>
        new(_playbacks.Object, _readings.Object, _media.Object);

    [Fact]
    public async Task Returns_empty_stats_when_no_history()
    {
        var profileId = Guid.NewGuid();
        _playbacks.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        _readings.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var svc = Build();
        var result = await svc.GetProfileStatisticsAsync(profileId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalItemsWatched.Should().Be(0);
        result.Value.TotalItemsRead.Should().Be(0);
        result.Value.TotalItemsCompleted.Should().Be(0);
        result.Value.TopGenres.Should().BeEmpty();
    }

    [Fact]
    public async Task Counts_completed_items_correctly()
    {
        var profileId = Guid.NewGuid();
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();

        var playbackList = new List<PlaybackProgress>
        {
            new() { ProfileId = profileId, MediaItemId = item1, IsCompleted = true, LastWatchedAt = DateTimeOffset.UtcNow, Duration = TimeSpan.FromMinutes(90) },
            new() { ProfileId = profileId, MediaItemId = item2, IsCompleted = false, LastWatchedAt = DateTimeOffset.UtcNow, Duration = TimeSpan.FromMinutes(90) },
        };

        _playbacks.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playbackList);
        _readings.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var svc = Build();
        var result = await svc.GetProfileStatisticsAsync(profileId);

        result.Value!.TotalItemsWatched.Should().Be(2);
        result.Value.TotalItemsCompleted.Should().Be(1);
    }

    [Fact]
    public async Task Builds_genre_breakdown_from_media_metadata()
    {
        var profileId = Guid.NewGuid();
        var action = new Genre("Action");
        var drama = new Genre("Drama");

        var movie1 = new VideoItem { LibraryId = Guid.NewGuid(), Title = "Film A" };
        movie1.Genres.Add(action);
        var movie2 = new VideoItem { LibraryId = Guid.NewGuid(), Title = "Film B" };
        movie2.Genres.Add(action);
        movie2.Genres.Add(drama);

        var playbacks = new List<PlaybackProgress>
        {
            new() { ProfileId = profileId, MediaItemId = movie1.Id, LastWatchedAt = DateTimeOffset.UtcNow },
            new() { ProfileId = profileId, MediaItemId = movie2.Id, LastWatchedAt = DateTimeOffset.UtcNow },
        };

        _playbacks.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playbacks);
        _readings.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([movie1, movie2]);

        var svc = Build();
        var result = await svc.GetProfileStatisticsAsync(profileId);

        result.IsSuccess.Should().BeTrue();
        var genres = result.Value!.TopGenres;
        genres.Should().HaveCountGreaterThanOrEqualTo(2);
        genres.First().Genre.Should().Be("Action");
        genres.First().Count.Should().Be(2);
    }

    [Fact]
    public async Task Weekly_activity_has_correct_week_count()
    {
        var profileId = Guid.NewGuid();
        _playbacks.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync([]);
        _readings.Setup(r => r.GetAllForProfileAsync(profileId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var svc = Build();
        var result = await svc.GetProfileStatisticsAsync(profileId, weekCount: 8);

        result.Value!.WeeklyActivity.Should().HaveCount(8);
    }
}
