using Mythra.Domain.Common;

namespace Mythra.Domain.Media.Audio;

public sealed class AudioChapter : Entity
{
    public Guid AudioItemId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Start { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Codec { get; set; }
    public long? Bitrate { get; set; }
}
