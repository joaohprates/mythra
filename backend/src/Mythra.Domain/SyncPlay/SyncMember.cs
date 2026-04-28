using Mythra.Domain.Common;

namespace Mythra.Domain.SyncPlay;

public sealed class SyncMember : Entity
{
    public Guid SyncRoomId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public TimeSpan Position { get; set; }
    public double Latency { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPingAt { get; set; }

    private SyncMember() { }

    public SyncMember(Guid roomId, Guid userId, bool isHost)
    {
        SyncRoomId = roomId;
        UserId = userId;
        IsHost = isHost;
    }

    public void Ping(double latency, TimeSpan position)
    {
        Latency = latency;
        Position = position;
        LastPingAt = DateTimeOffset.UtcNow;
    }
}
