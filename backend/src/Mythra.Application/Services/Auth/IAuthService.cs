using Mythra.Application.Dtos.Auth;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest req, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest req, string? userAgent, string? ip, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken, string? userAgent, string? ip, CancellationToken ct = default);
    Task<Result> LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<UserDto>> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest req, CancellationToken ct = default);
    Task<Result<ProfileDto>> CreateProfileAsync(Guid userId, CreateProfileRequest req, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ProfileDto>>> ListProfilesAsync(Guid userId, CancellationToken ct = default);
    Task<Result> DeleteProfileAsync(Guid userId, Guid profileId, CancellationToken ct = default);
}
