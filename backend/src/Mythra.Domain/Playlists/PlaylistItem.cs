using Mythra.Domain.Common;

namespace Mythra.Domain.Playlists;

public sealed class PlaylistItem : Entity
{
    public Guid PlaylistId { get; private set; }
    public Guid MediaItemId { get; private set; }
    public int Order { get; private set; }
    public DateTimeOffset AddedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PlaylistItem() { }

    internal PlaylistItem(Guid playlistId, Guid mediaItemId, int order)
    {
        PlaylistId = playlistId;
        MediaItemId = mediaItemId;
        Order = order;
    }

    internal void SetOrder(int order) => Order = order;
}
