using Mythra.Domain.Common;

namespace Mythra.Domain.Progress;

public sealed class PlaybackProgress : Entity
{
    public Guid ProfileId { get; set; }
    public Guid MediaItemId { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan? Duration { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset LastWatchedAt { get; set; } = DateTimeOffset.UtcNow;
    public int? PreferredAudioStreamIndex { get; set; }
    public int? PreferredSubtitleStreamIndex { get; set; }
    public double PlaybackSpeed { get; set; } = 1.0;

    public double PercentComplete => Duration is { TotalSeconds: > 0 } d
        ? Math.Clamp(Position.TotalSeconds / d.TotalSeconds * 100.0, 0, 100)
        : 0;

    public void UpdatePosition(TimeSpan position, TimeSpan? duration = null)
    {
        Position = position;
        if (duration.HasValue) Duration = duration;
        LastWatchedAt = DateTimeOffset.UtcNow;
        if (Duration is { } d && position >= d - TimeSpan.FromSeconds(15))
            IsCompleted = true;
        Touch();
    }
}
