using Mythra.Domain.Common;

namespace Mythra.Domain.Media.Video;

public enum SubtitleKind
{
    Embedded = 1,
    External = 2,
    Forced = 3,
    SDH = 4,
    Commentary = 5,
}

public sealed class Subtitle : Entity
{
    public Guid VideoItemId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? FilePath { get; set; }
    public int? StreamIndex { get; set; }
    public string Format { get; set; } = "srt";
    public SubtitleKind Kind { get; set; } = SubtitleKind.External;
    public bool IsDefault { get; set; }
    public bool IsForced { get; set; }
}
