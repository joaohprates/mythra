using Mythra.Application.Dtos.Auth;
using Mythra.Domain.Users;

namespace Mythra.Application.Mapping;

public static class AuthMappings
{
    public static UserDto ToDto(this User u) => new(
        u.Id,
        u.Email,
        u.Username,
        u.Role,
        u.AvatarPath,
        u.PreferredLanguage,
        u.Profiles.Select(p => p.ToDto()).ToList());

    public static ProfileDto ToDto(this Profile p) => new(
        p.Id,
        p.Name,
        p.AvatarPath,
        p.IsKidFriendly,
        p.Theme);
}
