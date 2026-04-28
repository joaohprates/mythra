using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Dtos.Auth;
using Mythra.Application.Services.Auth;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(req, ct);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await auth.LoginAsync(req, ua, ip, ct);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await auth.RefreshAsync(req.RefreshToken, ua, ip, ct);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var result = await auth.LogoutAsync(req.RefreshToken, ct);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await auth.GetMeAsync(userId, ct);
        return result.ToActionResult();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await auth.ChangePasswordAsync(userId, req, ct);
        return result.ToActionResult();
    }

    [HttpGet("profiles")]
    [Authorize]
    public async Task<IActionResult> ListProfiles(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await auth.ListProfilesAsync(userId, ct);
        return result.ToActionResult();
    }

    [HttpPost("profiles")]
    [Authorize]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await auth.CreateProfileAsync(userId, req, ct);
        return result.ToActionResult();
    }

    [HttpDelete("profiles/{profileId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteProfile(Guid profileId, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var result = await auth.DeleteProfileAsync(userId, profileId, ct);
        return result.ToActionResult();
    }
}
