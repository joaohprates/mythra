using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;

namespace Mythra.Domain.SyncPlay;

public sealed class SyncRoom : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public Guid HostUserId { get; private set; }
    public Guid? CurrentMediaItemId { get; set; }
    public TimeSpan CurrentPosition { get; set; }
    public bool IsPlaying { get; set; }
    public DateTimeOffset LastPositionUpdateAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsClosed { get; private set; }

    public List<SyncMember> Members { get; set; } = [];

    private SyncRoom() { }

    public SyncRoom(string name, Guid hostUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvariantViolationException("Room name is required.");
        Name = name.Trim();
        HostUserId = hostUserId;
        Code = GenerateCode();
        Members.Add(new SyncMember(Id, hostUserId, isHost: true));
    }

    public void Join(Guid userId, string displayName)
    {
        if (IsClosed) throw new InvariantViolationException("Room is closed.");
        if (Members.Any(m => m.UserId == userId)) return;
        Members.Add(new SyncMember(Id, userId, isHost: false) { DisplayName = displayName });
        Touch();
    }

    public void Leave(Guid userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null) return;
        Members.Remove(member);
        if (Members.Count == 0) Close();
        Touch();
    }

    public void Close()
    {
        IsClosed = true;
        IsPlaying = false;
        Touch();
    }

    public void RecordCommand(SyncCommandKind kind, TimeSpan position, Guid? mediaItemId = null)
    {
        CurrentPosition = position;
        if (mediaItemId.HasValue) CurrentMediaItemId = mediaItemId;
        IsPlaying = kind switch
        {
            SyncCommandKind.Play or SyncCommandKind.Resume or SyncCommandKind.Seek => true,
            SyncCommandKind.Pause or SyncCommandKind.Stop => false,
            _ => IsPlaying,
        };
        LastPositionUpdateAt = DateTimeOffset.UtcNow;
        Touch();
    }

    private static string GenerateCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = new byte[6];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new string([.. bytes.Select(b => alphabet[b % alphabet.Length])]);
    }
}
