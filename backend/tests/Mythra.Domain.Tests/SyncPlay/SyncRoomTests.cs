using FluentAssertions;
using Mythra.Domain.SyncPlay;

namespace Mythra.Domain.Tests.SyncPlay;

public class SyncRoomTests
{
    [Fact]
    public void Construction_creates_host_member_and_unique_code()
    {
        var hostId = Guid.NewGuid();
        var room = new SyncRoom("Watch party", hostId);
        room.Code.Should().HaveLength(6);
        room.Members.Should().ContainSingle().Which.UserId.Should().Be(hostId);
        room.Members[0].IsHost.Should().BeTrue();
    }

    [Fact]
    public void Join_idempotent_for_same_user()
    {
        var room = new SyncRoom("Party", Guid.NewGuid());
        var u = Guid.NewGuid();
        room.Join(u, "Alice");
        room.Join(u, "Alice");
        room.Members.Should().HaveCount(2);
    }

    [Fact]
    public void Leave_closes_room_when_no_members_remain()
    {
        var host = Guid.NewGuid();
        var room = new SyncRoom("Party", host);
        room.Leave(host);
        room.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void RecordCommand_updates_state_and_position()
    {
        var room = new SyncRoom("Party", Guid.NewGuid());
        room.RecordCommand(SyncCommandKind.Play, TimeSpan.FromSeconds(42));
        room.IsPlaying.Should().BeTrue();
        room.CurrentPosition.Should().Be(TimeSpan.FromSeconds(42));

        room.RecordCommand(SyncCommandKind.Pause, TimeSpan.FromSeconds(50));
        room.IsPlaying.Should().BeFalse();
        room.CurrentPosition.Should().Be(TimeSpan.FromSeconds(50));
    }
}
