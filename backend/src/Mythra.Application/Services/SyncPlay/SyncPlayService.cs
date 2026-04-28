using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.SyncPlay;
using Mythra.Domain.Common;
using Mythra.Domain.SyncPlay;

namespace Mythra.Application.Services.SyncPlay;

public sealed class SyncPlayService(
    ISyncRoomRepository rooms,
    IUnitOfWork uow) : ISyncPlayService
{
    public async Task<Result<SyncRoomDto>> CreateRoomAsync(Guid userId, string username, CreateSyncRoomRequest req, CancellationToken ct = default)
    {
        var room = new SyncRoom(req.Name, userId);
        if (req.InitialMediaItemId.HasValue) room.CurrentMediaItemId = req.InitialMediaItemId;
        var host = room.Members.First();
        host.DisplayName = username;
        await rooms.AddAsync(room, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(room);
    }

    public async Task<Result<SyncRoomDto>> JoinAsync(Guid userId, JoinSyncRoomRequest req, CancellationToken ct = default)
    {
        var room = await rooms.GetByCodeAsync(req.Code, ct);
        if (room is null) return Error.NotFound("SyncRoom", req.Code);
        if (room.IsClosed) return Error.Validation("Room is closed.");
        room.Join(userId, req.DisplayName);
        await uow.SaveChangesAsync(ct);
        return ToDto(room);
    }

    public async Task<Result> LeaveAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var room = await rooms.GetByCodeAsync(code, ct);
        if (room is null) return Error.NotFound("SyncRoom", code);
        room.Leave(userId);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<SyncRoomDto>> GetAsync(string code, CancellationToken ct = default)
    {
        var room = await rooms.GetByCodeAsync(code, ct);
        return room is null ? Error.NotFound("SyncRoom", code) : ToDto(room);
    }

    public async Task<Result> ApplyCommandAsync(Guid userId, string code, SyncCommandDto cmd, CancellationToken ct = default)
    {
        var room = await rooms.GetByCodeAsync(code, ct);
        if (room is null) return Error.NotFound("SyncRoom", code);
        if (room.HostUserId != userId)
            return Error.Unauthorized("Only the host may issue sync commands.");
        room.RecordCommand(cmd.Kind, cmd.Position, cmd.MediaItemId);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PingAsync(Guid userId, string code, double latencyMs, TimeSpan position, CancellationToken ct = default)
    {
        var room = await rooms.GetWithMembersAsync((await rooms.GetByCodeAsync(code, ct))?.Id ?? Guid.Empty, ct);
        if (room is null) return Error.NotFound("SyncRoom", code);
        var member = room.Members.FirstOrDefault(m => m.UserId == userId);
        if (member is null) return Error.NotFound("SyncMember", userId);
        member.Ping(latencyMs, position);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static SyncRoomDto ToDto(SyncRoom r) => new(
        r.Id,
        r.Code,
        r.Name,
        r.HostUserId,
        r.CurrentMediaItemId,
        r.CurrentPosition,
        r.IsPlaying,
        r.Members.Select(m => new SyncMemberDto(m.UserId, m.DisplayName, m.IsHost, m.IsReady, m.Latency)).ToList());
}
