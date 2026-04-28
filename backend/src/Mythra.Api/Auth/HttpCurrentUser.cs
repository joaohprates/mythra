using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Mythra.Application.Abstractions.Auth;
using Mythra.Domain.Users;

namespace Mythra.Api.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var sub = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Username => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public UserRole? Role
    {
        get
        {
            var role = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(role, out var r) ? r : null;
        }
    }

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
