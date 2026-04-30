using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Addons;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class AddonRepository(MythraDbContext db) : EfRepository<Addon>(db), IAddonRepository
{
    public Task<IReadOnlyList<Addon>> ListByUserAsync(Guid userId, CancellationToken ct = default) =>
        Set.Where(a => a.UserId == userId).OrderBy(a => a.Name).ToListAsync(ct)
           .ContinueWith(t => (IReadOnlyList<Addon>)t.Result, ct);
}
