using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;

namespace Mythra.Domain.Playlists;

public sealed class Playlist : AggregateRoot
{
    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsPublic { get; private set; }
    public string? CoverImagePath { get; private set; }

    private readonly List<PlaylistItem> _items = [];
    public IReadOnlyList<PlaylistItem> Items => _items.AsReadOnly();

    private Playlist() { }

    public Playlist(Guid profileId, string name, string? description = null, bool isPublic = false)
    {
        if (profileId == Guid.Empty)
            throw new InvariantViolationException("Playlist requires a profile.");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvariantViolationException("Playlist name cannot be empty.");

        ProfileId = profileId;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsPublic = isPublic;
    }

    public void Rename(string name, string? description)
    {
        Name = name.Trim();
        Description = description?.Trim();
        Touch();
    }

    public void SetVisibility(bool isPublic)
    {
        IsPublic = isPublic;
        Touch();
    }

    public void SetCover(string? path)
    {
        CoverImagePath = path;
        Touch();
    }

    public PlaylistItem AddItem(Guid mediaItemId)
    {
        if (_items.Any(i => i.MediaItemId == mediaItemId))
            return _items.First(i => i.MediaItemId == mediaItemId);

        var order = _items.Count == 0 ? 0 : _items.Max(i => i.Order) + 1;
        var item = new PlaylistItem(Id, mediaItemId, order);
        _items.Add(item);
        Touch();
        return item;
    }

    public void RemoveItem(Guid mediaItemId)
    {
        var item = _items.FirstOrDefault(i => i.MediaItemId == mediaItemId);
        if (item is null) return;
        _items.Remove(item);
        ReorderItems();
        Touch();
    }

    public void ReorderItem(Guid mediaItemId, int newOrder)
    {
        var item = _items.FirstOrDefault(i => i.MediaItemId == mediaItemId);
        if (item is null) return;

        var clamped = Math.Clamp(newOrder, 0, _items.Count - 1);
        _items.Remove(item);
        _items.Insert(clamped, item);
        ReorderItems();
        Touch();
    }

    private void ReorderItems()
    {
        for (var i = 0; i < _items.Count; i++)
            _items[i].SetOrder(i);
    }
}
