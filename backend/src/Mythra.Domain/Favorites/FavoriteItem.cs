using Mythra.Domain.Common;

namespace Mythra.Domain.Favorites;

public sealed class FavoriteItem : Entity
{
    public Guid ProfileId { get; private set; }
    public Guid MediaItemId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private FavoriteItem() { }

    public static FavoriteItem Create(Guid profileId, Guid mediaItemId)
    {
        return new FavoriteItem
        {
            ProfileId = profileId,
            MediaItemId = mediaItemId,
            AddedAt = DateTime.UtcNow,
        };
    }
}
