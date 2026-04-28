using FluentAssertions;
using Mythra.Domain.Progress;

namespace Mythra.Domain.Tests.Progress;

public class PlaybackProgressTests
{
    [Fact]
    public void UpdatePosition_records_time_and_clamps_completion_at_end()
    {
        var p = new PlaybackProgress();
        p.UpdatePosition(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120));
        p.IsCompleted.Should().BeFalse();
        p.PercentComplete.Should().BeApproximately(25, 1);

        p.UpdatePosition(TimeSpan.FromSeconds(115), TimeSpan.FromSeconds(120));
        p.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Percent_complete_zero_when_no_duration()
    {
        var p = new PlaybackProgress();
        p.UpdatePosition(TimeSpan.FromSeconds(5));
        p.PercentComplete.Should().Be(0);
    }
}

public class ReadingProgressTests
{
    [Fact]
    public void UpdateProgress_clamps_percent_and_marks_completion()
    {
        var p = new ReadingProgress();
        p.UpdateProgress(50, 100);
        p.PercentComplete.Should().Be(50);
        p.CurrentPage.Should().Be(100);
        p.IsCompleted.Should().BeFalse();

        p.UpdateProgress(150);
        p.PercentComplete.Should().Be(100);
        p.IsCompleted.Should().BeTrue();
    }
}
