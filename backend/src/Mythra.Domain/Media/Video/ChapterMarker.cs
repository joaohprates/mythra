using Mythra.Domain.Common;

namespace Mythra.Domain.Media.Video;

public enum ChapterMarkerKind
{
    Generic = 0,
    IntroStart = 1,
    IntroEnd = 2,
    OutroStart = 3,
    OutroEnd = 4,
    RecapStart = 5,
    RecapEnd = 6,
    PreviewStart = 7,
    PreviewEnd = 8,
}

public sealed class ChapterMarker : Entity
{
    public Guid VideoItemId { get; set; }
    public ChapterMarkerKind Kind { get; set; }
    public string? Label { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan? End { get; set; }
    public string? ThumbPath { get; set; }
}
