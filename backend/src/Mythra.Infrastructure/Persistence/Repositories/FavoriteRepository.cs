using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Favorites;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class FavoriteRepository(MythraDbContext db) : IFavoriteRepository
{
    public async Task<IEnumerable<FavoriteItem>> GetByProfileAsync(Guid profileId, CancellationToken ct = default)
        => await db.Favorites
                   .Where(f => f.ProfileId == profileId)
                   .OrderByDescending(f => f.AddedAt)
                   .ToListAsync(ct);

    public Task<FavoriteItem?> GetAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default)
        => db.Favorites.FirstOrDefaultAsync(
            f => f.ProfileId == profileId && f.MediaItemId == mediaItemId, ct);

    public Task<bool> ExistsAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default)
        => db.Favorites.AnyAsync(
            f => f.ProfileId == profileId && f.MediaItemId == mediaItemId, ct);

    public void Add(FavoriteItem item)
        => db.Favorites.Add(item);

    public void Remove(FavoriteItem item)
        => db.Favorites.Remove(item);
}
