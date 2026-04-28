using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Users;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(MythraDbContext db) : EfRepository<User>(db), IUserRepository
{
    public override Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(u => u.Profiles).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Set.Include(u => u.Profiles).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        Set.Include(u => u.Profiles).FirstOrDefaultAsync(u => u.Username == username, ct);

    public Task<bool> ExistsAsync(string email, CancellationToken ct = default) =>
        Set.AnyAsync(u => u.Email == email, ct);

    public Task<int> CountAsync(CancellationToken ct = default) => Set.CountAsync(ct);
}

public sealed class ProfileRepository(MythraDbContext db) : EfRepository<Profile>(db), IProfileRepository
{
    public async Task<IReadOnlyList<Profile>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
        await Set.Where(p => p.UserId == userId).OrderBy(p => p.Name).ToListAsync(ct);
}

public sealed class SessionRepository(MythraDbContext db) : EfRepository<Session>(db), ISessionRepository
{
    public Task<Session?> GetByRefreshHashAsync(string hash, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, ct);

    public async Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await Set.Where(s => s.UserId == userId && s.RevokedAt == null).ToListAsync(ct);
        foreach (var s in rows) s.Revoke();
        return rows.Count;
    }
}
