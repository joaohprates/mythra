using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;

namespace Mythra.Domain.Users;

public sealed class User : AggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; private set; }
    public string? AvatarPath { get; set; }
    public string PreferredLanguage { get; set; } = "en";

    public List<Profile> Profiles { get; set; } = [];

    private User() { }

    public static User Register(string email, string username, string passwordHash, UserRole role = UserRole.User)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new InvariantViolationException("Invalid email.");
        if (string.IsNullOrWhiteSpace(username))
            throw new InvariantViolationException("Username is required.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new InvariantViolationException("Password hash is required.");

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            Username = username.Trim(),
            PasswordHash = passwordHash,
            Role = role,
        };
    }

    public void RecordLogin() => LastLoginAt = DateTimeOffset.UtcNow;

    public void ChangePassword(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new InvariantViolationException("Password hash is required.");
        PasswordHash = newHash;
        Touch();
    }

    public Profile AddProfile(string name)
    {
        var profile = new Profile(Id, name);
        Profiles.Add(profile);
        Touch();
        return profile;
    }
}
