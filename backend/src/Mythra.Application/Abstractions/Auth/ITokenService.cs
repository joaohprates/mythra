using Mythra.Domain.Users;

namespace Mythra.Application.Abstractions.Auth;

public sealed record TokenPair(string AccessToken, DateTimeOffset AccessExpiresAt, string RefreshToken, DateTimeOffset RefreshExpiresAt);

public interface ITokenService
{
    TokenPair IssueFor(User user);
    string ComputeRefreshHash(string refreshToken);
}

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Username { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
