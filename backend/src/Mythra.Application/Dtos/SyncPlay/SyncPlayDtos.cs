using Mythra.Domain.SyncPlay;

namespace Mythra.Application.Dtos.SyncPlay;

public sealed record CreateSyncRoomRequest(string Name, Guid? InitialMediaItemId);

public sealed record JoinSyncRoomRequest(string Code, string DisplayName);

public sealed record SyncRoomDto(
    Guid Id,
    string Code,
    string Name,
    Guid HostUserId,
    Guid? CurrentMediaItemId,
    TimeSpan CurrentPosition,
    bool IsPlaying,
    IReadOnlyList<SyncMemberDto> Members);

public sealed record SyncMemberDto(Guid UserId, string DisplayName, bool IsHost, bool IsReady, double Latency);

public sealed record SyncCommandDto(SyncCommandKind Kind, TimeSpan Position, Guid? MediaItemId);
