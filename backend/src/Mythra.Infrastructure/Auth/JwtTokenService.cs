using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Time;
using Mythra.Domain.Users;

namespace Mythra.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private readonly JwtOptions _opts = options.Value;

    public TokenPair IssueFor(User user)
    {
        var now = clock.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var accessExp = now.AddMinutes(_opts.AccessTokenMinutes);
        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExp.UtcDateTime,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = GenerateRefreshToken();
        var refreshExp = now.AddDays(_opts.RefreshTokenDays);

        return new TokenPair(access, accessExp, refresh, refreshExp);
    }

    public string ComputeRefreshHash(string refreshToken)
    {
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('/', '_').Replace('+', '-');
    }
}
