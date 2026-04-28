using Mythra.Domain.Common;

namespace Mythra.Domain.Media.Video;

public sealed class AudioTrack : Entity
{
    public Guid VideoItemId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public int StreamIndex { get; set; }
    public string Codec { get; set; } = string.Empty;
    public int Channels { get; set; } = 2;
    public string ChannelLayout { get; set; } = "stereo";
    public int SampleRate { get; set; }
    public long? Bitrate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsCommentary { get; set; }
}
