using Mythra.Application.Dtos.SyncPlay;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.SyncPlay;

public interface ISyncPlayService
{
    Task<Result<SyncRoomDto>> CreateRoomAsync(Guid userId, string username, CreateSyncRoomRequest req, CancellationToken ct = default);
    Task<Result<SyncRoomDto>> JoinAsync(Guid userId, JoinSyncRoomRequest req, CancellationToken ct = default);
    Task<Result> LeaveAsync(Guid userId, string code, CancellationToken ct = default);
    Task<Result<SyncRoomDto>> GetAsync(string code, CancellationToken ct = default);
    Task<Result> ApplyCommandAsync(Guid userId, string code, SyncCommandDto cmd, CancellationToken ct = default);
    Task<Result> PingAsync(Guid userId, string code, double latencyMs, TimeSpan position, CancellationToken ct = default);
}
