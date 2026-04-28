using Mythra.Domain.Common;

namespace Mythra.Domain.Streaming;

public enum StreamState
{
    Initializing = 0,
    Ready = 1,
    Active = 2,
    Paused = 3,
    Ended = 4,
    Failed = 5,
}

public enum StreamMode
{
    DirectPlay = 1,
    Remux = 2,
    Transcode = 3,
}

public sealed class StreamSession : AggregateRoot
{
    public Guid UserId { get; set; }
    public Guid ProfileId { get; set; }
    public Guid VideoItemId { get; set; }
    public StreamMode Mode { get; set; }
    public StreamState State { get; set; } = StreamState.Initializing;
    public string SessionToken { get; set; } = Guid.NewGuid().ToString("N");
    public string? PlaylistPath { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? Bitrate { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public void MarkReady(string playlistPath)
    {
        PlaylistPath = playlistPath;
        State = StreamState.Ready;
        Touch();
    }

    public void MarkActive() { State = StreamState.Active; Touch(); }
    public void MarkPaused() { State = StreamState.Paused; Touch(); }

    public void End()
    {
        State = StreamState.Ended;
        EndedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Fail(string reason)
    {
        State = StreamState.Failed;
        FailureReason = reason;
        EndedAt = DateTimeOffset.UtcNow;
        Touch();
    }
}
