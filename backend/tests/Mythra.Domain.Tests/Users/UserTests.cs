using FluentAssertions;
using Mythra.Domain.Common.Errors;
using Mythra.Domain.Users;

namespace Mythra.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Register_normalizes_email_and_creates_user()
    {
        var user = User.Register("Foo@Bar.COM", "foo", "hashed");
        user.Email.Should().Be("foo@bar.com");
        user.Username.Should().Be("foo");
        user.Role.Should().Be(UserRole.User);
    }

    [Theory]
    [InlineData("not-email", "user", "hash")]
    [InlineData("foo@bar.com", "", "hash")]
    [InlineData("foo@bar.com", "user", "")]
    public void Register_rejects_invalid_inputs(string email, string username, string hash)
    {
        Action act = () => User.Register(email, username, hash);
        act.Should().Throw<InvariantViolationException>();
    }

    [Fact]
    public void ChangePassword_with_empty_throws()
    {
        var user = User.Register("a@b.com", "u", "hash");
        Action act = () => user.ChangePassword("");
        act.Should().Throw<InvariantViolationException>();
    }

    [Fact]
    public void RecordLogin_sets_LastLoginAt()
    {
        var user = User.Register("a@b.com", "u", "hash");
        user.LastLoginAt.Should().BeNull();
        user.RecordLogin();
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public void AddProfile_appends_profile_with_user_id()
    {
        var user = User.Register("a@b.com", "u", "hash");
        var p = user.AddProfile("Main");
        p.UserId.Should().Be(user.Id);
        p.Name.Should().Be("Main");
        user.Profiles.Should().Contain(p);
    }
}
