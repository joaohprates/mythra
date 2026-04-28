namespace Mythra.Domain.SyncPlay;

public enum SyncCommandKind
{
    Play = 1,
    Pause = 2,
    Resume = 3,
    Seek = 4,
    Stop = 5,
    ChangeMedia = 6,
    Buffer = 7,
    Ready = 8,
}

public sealed record SyncCommand(
    Guid RoomId,
    Guid IssuedByUserId,
    SyncCommandKind Kind,
    TimeSpan Position,
    Guid? MediaItemId,
    DateTimeOffset At);
