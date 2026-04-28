using Mythra.Domain.Users;

namespace Mythra.Application.Dtos.Auth;

public sealed record RegisterRequest(string Email, string Username, string Password);

public sealed record LoginRequest(string EmailOrUsername, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    UserDto User);

public sealed record UserDto(
    Guid Id,
    string Email,
    string Username,
    UserRole Role,
    string? AvatarPath,
    string PreferredLanguage,
    IReadOnlyList<ProfileDto> Profiles);

public sealed record ProfileDto(
    Guid Id,
    string Name,
    string? AvatarPath,
    bool IsKidFriendly,
    string Theme);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record CreateProfileRequest(string Name, bool IsKidFriendly = false);
