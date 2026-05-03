using Mythra.Domain.Favorites;
using Mythra.Domain.Media;

namespace Mythra.Application.Abstractions.Persistence;

public interface IFavoriteRepository
{
    Task<IEnumerable<FavoriteItem>> GetByProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<FavoriteItem?> GetAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    void Add(FavoriteItem item);
    void Remove(FavoriteItem item);
}
