using FluentAssertions;
using Mythra.Domain.Media.Video;

namespace Mythra.Domain.Tests.Media;

public class VideoItemTests
{
    [Theory]
    [InlineData(3840, 2160, "4K")]
    [InlineData(1920, 1080, "1080p")]
    [InlineData(1280, 720, "720p")]
    [InlineData(854, 480, "480p")]
    [InlineData(640, 360, "SD")]
    [InlineData(null, null, "SD")]
    public void Resolution_label_picks_correct_bucket(int? w, int? h, string expected)
    {
        var v = new VideoItem { Title = "T", Width = w, Height = h };
        v.ResolutionLabel.Should().Be(expected);
    }

    [Fact]
    public void Kind_is_Video()
    {
        var v = new VideoItem { Title = "T" };
        v.Kind.Should().Be(Mythra.Domain.Media.MediaKind.Video);
    }
}
