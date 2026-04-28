using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Auth;
using Mythra.Application.Services.Auth;
using Mythra.Domain.Users;

namespace Mythra.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProfileRepository> _profiles = new();
    private readonly Mock<ISessionRepository> _sessions = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AuthService Build()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => "hash:" + p);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
               .Returns<string, string>((p, h) => h == "hash:" + p);
        _tokens.Setup(t => t.IssueFor(It.IsAny<User>()))
               .Returns(new TokenPair("access", DateTimeOffset.UtcNow.AddMinutes(30), "refresh", DateTimeOffset.UtcNow.AddDays(30)));
        _tokens.Setup(t => t.ComputeRefreshHash(It.IsAny<string>())).Returns<string>(s => "h:" + s);
        return new AuthService(_users.Object, _profiles.Object, _sessions.Object, _hasher.Object, _tokens.Object, _uow.Object, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Register_first_user_becomes_admin()
    {
        _users.Setup(u => u.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(u => u.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var svc = Build();
        var result = await svc.RegisterAsync(new RegisterRequest("a@b.com", "alice", "Hunter11!"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.User.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_conflict()
    {
        _users.Setup(u => u.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var svc = Build();
        var result = await svc.RegisterAsync(new RegisterRequest("a@b.com", "alice", "Hunter11!"));
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("conflict");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_unauthorized()
    {
        var user = User.Register("a@b.com", "alice", "hash:Hunter11!");
        _users.Setup(u => u.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var svc = Build();
        var result = await svc.LoginAsync(new LoginRequest("a@b.com", "wrong-password"), null, null);
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("unauthorized");
    }

    [Fact]
    public async Task Login_with_username_succeeds()
    {
        var user = User.Register("a@b.com", "alice", "hash:Hunter11!");
        _users.Setup(u => u.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var svc = Build();
        var result = await svc.LoginAsync(new LoginRequest("alice", "Hunter11!"), null, null);
        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().NotBeEmpty();
    }
}
