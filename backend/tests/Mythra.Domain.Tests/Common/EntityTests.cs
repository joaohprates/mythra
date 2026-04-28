using FluentAssertions;
using Mythra.Domain.Common;

namespace Mythra.Domain.Tests.Common;

public class EntityTests
{
    private sealed class TestEntity : Entity { }
    private sealed class OtherEntity : Entity { }

    [Fact]
    public void Two_entities_with_same_id_and_type_are_equal()
    {
        var a = new TestEntity();
        var b = new TestEntity();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(b, a.Id);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Different_types_with_same_id_are_not_equal()
    {
        var a = new TestEntity();
        var b = new OtherEntity();
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(b, a.Id);
        a.Should().NotBe(b);
    }

    [Fact]
    public void Touch_sets_updated_at()
    {
        var a = new TestEntity();
        a.UpdatedAt.Should().BeNull();
        a.Touch();
        a.UpdatedAt.Should().NotBeNull();
    }
}
