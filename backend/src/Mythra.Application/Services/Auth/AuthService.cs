using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Auth;
using Mythra.Application.Mapping;
using Mythra.Domain.Common;
using Mythra.Domain.Users;

namespace Mythra.Application.Services.Auth;

public sealed class AuthService(
    IUserRepository users,
    IProfileRepository profiles,
    ISessionRepository sessions,
    IPasswordHasher hasher,
    ITokenService tokens,
    IUnitOfWork uow,
    ILogger<AuthService> log) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await users.ExistsAsync(email, ct))
            return Error.Conflict("Email already registered.");

        var existingByUsername = await users.GetByUsernameAsync(req.Username, ct);
        if (existingByUsername is not null)
            return Error.Conflict("Username already taken.");

        var role = await users.CountAsync(ct) == 0 ? UserRole.Admin : UserRole.User;
        var user = User.Register(email, req.Username, hasher.Hash(req.Password), role);
        user.AddProfile(req.Username);

        await users.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        log.LogInformation("Registered user {Email} as {Role}", email, role);
        return await IssueTokensAsync(user, null, null, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest req, string? userAgent, string? ip, CancellationToken ct = default)
    {
        var user = req.EmailOrUsername.Contains('@')
            ? await users.GetByEmailAsync(req.EmailOrUsername.Trim().ToLowerInvariant(), ct)
            : await users.GetByUsernameAsync(req.EmailOrUsername.Trim(), ct);

        if (user is null || !user.IsActive)
            return Error.Unauthorized("Invalid credentials.");
        if (!hasher.Verify(req.Password, user.PasswordHash))
            return Error.Unauthorized("Invalid credentials.");

        if (hasher.NeedsRehash(user.PasswordHash))
            user.ChangePassword(hasher.Hash(req.Password));

        user.RecordLogin();
        return await IssueTokensAsync(user, userAgent, ip, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, string? userAgent, string? ip, CancellationToken ct = default)
    {
        var hash = tokens.ComputeRefreshHash(refreshToken);
        var session = await sessions.GetByRefreshHashAsync(hash, ct);
        if (session is null || !session.IsActive)
            return Error.Unauthorized("Refresh token invalid or expired.");

        var user = await users.GetByIdAsync(session.UserId, ct);
        if (user is null || !user.IsActive)
            return Error.Unauthorized("User no longer active.");

        session.Revoke();
        return await IssueTokensAsync(user, userAgent, ip, ct);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokens.ComputeRefreshHash(refreshToken);
        var session = await sessions.GetByRefreshHashAsync(hash, ct);
        if (session is null) return Result.Success();
        session.Revoke();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        return user is null ? Error.NotFound("User", userId) : user.ToDto();
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Error.NotFound("User", userId);
        if (!hasher.Verify(req.CurrentPassword, user.PasswordHash))
            return Error.Unauthorized("Current password is incorrect.");
        user.ChangePassword(hasher.Hash(req.NewPassword));
        await sessions.RevokeAllForUserAsync(userId, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProfileDto>> CreateProfileAsync(Guid userId, CreateProfileRequest req, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Error.NotFound("User", userId);
        var profile = user.AddProfile(req.Name);
        profile.IsKidFriendly = req.IsKidFriendly;
        await uow.SaveChangesAsync(ct);
        return profile.ToDto();
    }

    public async Task<Result<IReadOnlyList<ProfileDto>>> ListProfilesAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await profiles.ListByUserAsync(userId, ct);
        return Result<IReadOnlyList<ProfileDto>>.Success(list.Select(p => p.ToDto()).ToList());
    }

    public async Task<Result> DeleteProfileAsync(Guid userId, Guid profileId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return Error.NotFound("User", userId);
        var profile = user.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return Error.NotFound("Profile", profileId);
        if (user.Profiles.Count == 1) return Error.Validation("Cannot delete the last profile.");
        user.Profiles.Remove(profile);
        profiles.Remove(profile);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> IssueTokensAsync(User user, string? userAgent, string? ip, CancellationToken ct)
    {
        var pair = tokens.IssueFor(user);
        var session = new Session
        {
            UserId = user.Id,
            RefreshTokenHash = tokens.ComputeRefreshHash(pair.RefreshToken),
            ExpiresAt = pair.RefreshExpiresAt,
            UserAgent = userAgent,
            IpAddress = ip,
        };
        await sessions.AddAsync(session, ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResponse(
            pair.AccessToken,
            pair.AccessExpiresAt,
            pair.RefreshToken,
            pair.RefreshExpiresAt,
            user.ToDto());
    }
}
