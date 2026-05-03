using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Playlists;
using Mythra.Application.Services.Playlists;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Video;
using Mythra.Domain.Playlists;

namespace Mythra.Application.Tests.Playlists;

public class PlaylistServiceTests
{
    private readonly Mock<IPlaylistRepository> _playlists = new();
    private readonly Mock<IMediaItemRepository> _media = new();
    private readonly Mock<IProfileRepository> _profiles = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PlaylistService Build()
    {
        // Default: profile lookup succeeds. Tests that need a missing-profile path
        // can override `_profiles.Setup(...)` before calling Build().
        _profiles.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Mythra.Domain.Users.Profile(Guid.NewGuid(), "Test"));
        return new(_playlists.Object, _media.Object, _profiles.Object, _uow.Object, NullLogger<PlaylistService>.Instance);
    }

    [Fact]
    public async Task Create_returns_playlist_detail()
    {
        var profileId = Guid.NewGuid();
        var req = new CreatePlaylistRequest("My List", "Description", false);

        var svc = Build();
        var result = await svc.CreateAsync(profileId, req);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("My List");
        result.Value.ProfileId.Should().Be(profileId);
        result.Value.IsPublic.Should().BeFalse();
        _playlists.Verify(r => r.AddAsync(It.Is<Playlist>(p => p.Name == "My List"), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_trims_name_whitespace()
    {
        var profileId = Guid.NewGuid();
        var svc = Build();
        var result = await svc.CreateAsync(profileId, new CreatePlaylistRequest("  Trimmed  "));
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Trimmed");
    }

    [Fact]
    public async Task Get_not_found_returns_error()
    {
        _playlists.Setup(r => r.GetWithItemsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Playlist?)null);

        var svc = Build();
        var result = await svc.GetAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task Get_private_playlist_as_other_profile_returns_forbidden()
    {
        var ownerProfile = Guid.NewGuid();
        var otherProfile = Guid.NewGuid();
        var playlist = new Playlist(ownerProfile, "Private", null, isPublic: false);

        _playlists.Setup(r => r.GetWithItemsAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);

        var svc = Build();
        var result = await svc.GetAsync(playlist.Id, otherProfile);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("forbidden");
    }

    [Fact]
    public async Task Get_public_playlist_as_other_profile_succeeds()
    {
        var ownerProfile = Guid.NewGuid();
        var otherProfile = Guid.NewGuid();
        var playlist = new Playlist(ownerProfile, "Public", null, isPublic: true);

        _playlists.Setup(r => r.GetWithItemsAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([]);

        var svc = Build();
        var result = await svc.GetAsync(playlist.Id, otherProfile);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddItem_not_found_media_returns_error()
    {
        var profileId = Guid.NewGuid();
        var playlist = new Playlist(profileId, "List");

        _playlists.Setup(r => r.GetWithItemsAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);
        _media.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((MediaItem?)null);

        var svc = Build();
        var result = await svc.AddItemAsync(playlist.Id, profileId, new AddPlaylistItemRequest(Guid.NewGuid()));

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task AddItem_adds_and_saves()
    {
        var profileId = Guid.NewGuid();
        var playlist = new Playlist(profileId, "List");
        var video = new VideoItem { LibraryId = Guid.NewGuid(), Title = "Test Movie" };

        _playlists.Setup(r => r.GetWithItemsAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);
        _media.Setup(m => m.GetByIdAsync(video.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(video);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([video]);

        var svc = Build();
        var result = await svc.AddItemAsync(playlist.Id, profileId, new AddPlaylistItemRequest(video.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].Title.Should().Be("Test Movie");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_wrong_profile_returns_forbidden()
    {
        var ownerProfile = Guid.NewGuid();
        var otherProfile = Guid.NewGuid();
        var playlist = new Playlist(ownerProfile, "Mine");

        _playlists.Setup(r => r.GetByIdAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);

        var svc = Build();
        var result = await svc.DeleteAsync(playlist.Id, otherProfile);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("forbidden");
        _playlists.Verify(r => r.Remove(It.IsAny<Playlist>()), Times.Never);
    }

    [Fact]
    public async Task Delete_owner_succeeds()
    {
        var profileId = Guid.NewGuid();
        var playlist = new Playlist(profileId, "Mine");

        _playlists.Setup(r => r.GetByIdAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);

        var svc = Build();
        var result = await svc.DeleteAsync(playlist.Id, profileId);

        result.IsSuccess.Should().BeTrue();
        _playlists.Verify(r => r.Remove(playlist), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_updates_order()
    {
        var profileId = Guid.NewGuid();
        var playlist = new Playlist(profileId, "List");
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var v1 = new VideoItem { LibraryId = Guid.NewGuid(), Title = "A" };
        var v2 = new VideoItem { LibraryId = Guid.NewGuid(), Title = "B" };
        var v3 = new VideoItem { LibraryId = Guid.NewGuid(), Title = "C" };

        _media.Setup(m => m.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Guid id, CancellationToken _) => id == v1.Id ? v1 : id == v2.Id ? v2 : v3);
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync([v1, v2, v3]);

        playlist.AddItem(v1.Id);
        playlist.AddItem(v2.Id);
        playlist.AddItem(v3.Id);

        _playlists.Setup(r => r.GetWithItemsAsync(playlist.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(playlist);

        var svc = Build();
        var result = await svc.RemoveItemAsync(playlist.Id, profileId, v2.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.Order).Should().Equal(0, 1);
    }
}
