using Mythra.Domain.Streaming;
using Mythra.Domain.SyncPlay;

namespace Mythra.Application.Abstractions.Persistence;

public interface IStreamSessionRepository : IRepository<StreamSession>
{
    Task<StreamSession?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<StreamSession>> ListActiveAsync(CancellationToken ct = default);
}

public interface ISyncRoomRepository : IRepository<SyncRoom>
{
    Task<SyncRoom?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<SyncRoom?> GetWithMembersAsync(Guid id, CancellationToken ct = default);
}
