using Mythra.Domain.Addons;

namespace Mythra.Application.Abstractions.Persistence;

public interface IAddonRepository
{
    Task AddAsync(Addon addon, CancellationToken ct = default);
    Task<Addon?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Addon>> ListByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns all Active addons across all users (used by startup activation).</summary>
    Task<IReadOnlyList<Addon>> ListActiveAsync(CancellationToken ct = default);

    void Remove(Addon addon);
}
