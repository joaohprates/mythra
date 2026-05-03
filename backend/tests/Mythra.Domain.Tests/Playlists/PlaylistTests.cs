using FluentAssertions;
using Mythra.Domain.Playlists;

namespace Mythra.Domain.Tests.Playlists;

public class PlaylistTests
{
    [Fact]
    public void AddItem_assigns_sequential_order()
    {
        var playlist = new Playlist(Guid.NewGuid(), "Test");
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        playlist.AddItem(id1);
        playlist.AddItem(id2);
        playlist.AddItem(id3);

        playlist.Items.Should().HaveCount(3);
        playlist.Items[0].Order.Should().Be(0);
        playlist.Items[1].Order.Should().Be(1);
        playlist.Items[2].Order.Should().Be(2);
    }

    [Fact]
    public void AddItem_duplicate_is_idempotent()
    {
        var playlist = new Playlist(Guid.NewGuid(), "Test");
        var id = Guid.NewGuid();

        playlist.AddItem(id);
        playlist.AddItem(id);

        playlist.Items.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveItem_reorders_remaining()
    {
        var playlist = new Playlist(Guid.NewGuid(), "Test");
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        playlist.AddItem(id1);
        playlist.AddItem(id2);
        playlist.AddItem(id3);

        playlist.RemoveItem(id2);

        playlist.Items.Should().HaveCount(2);
        playlist.Items[0].MediaItemId.Should().Be(id1);
        playlist.Items[0].Order.Should().Be(0);
        playlist.Items[1].MediaItemId.Should().Be(id3);
        playlist.Items[1].Order.Should().Be(1);
    }

    [Fact]
    public void ReorderItem_moves_to_correct_position()
    {
        var playlist = new Playlist(Guid.NewGuid(), "Test");
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        playlist.AddItem(id1);
        playlist.AddItem(id2);
        playlist.AddItem(id3);

        playlist.ReorderItem(id3, 0);

        playlist.Items[0].MediaItemId.Should().Be(id3);
        playlist.Items[0].Order.Should().Be(0);
        playlist.Items[1].MediaItemId.Should().Be(id1);
        playlist.Items[2].MediaItemId.Should().Be(id2);
    }

    [Fact]
    public void Rename_trims_and_updates()
    {
        var playlist = new Playlist(Guid.NewGuid(), "Old Name");
        playlist.Rename("  New Name  ", "desc");
        playlist.Name.Should().Be("New Name");
        playlist.Description.Should().Be("desc");
        playlist.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetVisibility_toggles_public()
    {
        var playlist = new Playlist(Guid.NewGuid(), "Test", isPublic: false);
        playlist.SetVisibility(true);
        playlist.IsPublic.Should().BeTrue();
    }
}
