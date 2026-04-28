using FluentAssertions;
using Mythra.Domain.Common.Errors;
using Mythra.Domain.Libraries;

namespace Mythra.Domain.Tests.Libraries;

public class LibraryTests
{
    [Fact]
    public void Construct_with_valid_args_succeeds()
    {
        var lib = new Library("Movies", LibraryKind.Video);
        lib.Name.Should().Be("Movies");
        lib.Kind.Should().Be(LibraryKind.Video);
        lib.IsEnabled.Should().BeTrue();
        lib.Folders.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Construct_with_invalid_name_throws(string? name)
    {
        Action act = () => _ = new Library(name!, LibraryKind.Video);
        act.Should().Throw<InvariantViolationException>();
    }

    [Fact]
    public void AddFolder_adds_unique_paths()
    {
        var lib = new Library("Movies", LibraryKind.Video);
        lib.AddFolder("/data/movies");
        lib.AddFolder("/data/extra");
        lib.Folders.Should().HaveCount(2);
    }

    [Fact]
    public void AddFolder_rejects_duplicate_path_case_insensitive()
    {
        var lib = new Library("Movies", LibraryKind.Video);
        lib.AddFolder("/data/movies");
        Action act = () => lib.AddFolder("/DATA/Movies");
        act.Should().Throw<InvariantViolationException>();
    }

    [Fact]
    public void Rename_updates_name_and_touches()
    {
        var lib = new Library("Movies", LibraryKind.Video);
        lib.Rename("Cinema");
        lib.Name.Should().Be("Cinema");
        lib.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Rename_with_empty_throws()
    {
        var lib = new Library("Movies", LibraryKind.Video);
        Action act = () => lib.Rename("  ");
        act.Should().Throw<InvariantViolationException>();
    }

    [Fact]
    public void MarkScanned_sets_lastScannedAt()
    {
        var lib = new Library("Movies", LibraryKind.Video);
        lib.LastScannedAt.Should().BeNull();
        lib.MarkScanned();
        lib.LastScannedAt.Should().NotBeNull();
    }
}
