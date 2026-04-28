using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Streaming;
using Mythra.Domain.SyncPlay;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class StreamSessionRepository(MythraDbContext db) : EfRepository<StreamSession>(db), IStreamSessionRepository
{
    public Task<StreamSession?> GetByTokenAsync(string token, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.SessionToken == token, ct);

    public async Task<IReadOnlyList<StreamSession>> ListActiveAsync(CancellationToken ct = default) =>
        await Set.Where(s => s.State == StreamState.Active || s.State == StreamState.Ready || s.State == StreamState.Paused)
                 .OrderByDescending(s => s.StartedAt)
                 .ToListAsync(ct);
}

public sealed class SyncRoomRepository(MythraDbContext db) : EfRepository<SyncRoom>(db), ISyncRoomRepository
{
    public Task<SyncRoom?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        Set.Include(r => r.Members).FirstOrDefaultAsync(r => r.Code == code, ct);

    public Task<SyncRoom?> GetWithMembersAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == id, ct);
}
