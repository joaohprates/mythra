using Mythra.Domain.Common;

namespace Mythra.Domain.Users;

public sealed class Session : Entity
{
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;

    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;
}
