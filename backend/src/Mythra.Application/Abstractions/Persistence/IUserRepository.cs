using Mythra.Domain.Users;

namespace Mythra.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> ExistsAsync(string email, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

public interface ISessionRepository : IRepository<Session>
{
    Task<Session?> GetByRefreshHashAsync(string refreshHash, CancellationToken ct = default);
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IProfileRepository : IRepository<Profile>
{
    Task<IReadOnlyList<Profile>> ListByUserAsync(Guid userId, CancellationToken ct = default);
}
