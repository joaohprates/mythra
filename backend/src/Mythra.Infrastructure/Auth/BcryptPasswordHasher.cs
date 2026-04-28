using Mythra.Application.Abstractions.Auth;

namespace Mythra.Infrastructure.Auth;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }

    public bool NeedsRehash(string hash)
    {
        try
        {
            var parts = hash.Split('$', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return true;
            return int.TryParse(parts[1], out var rounds) && rounds < WorkFactor;
        }
        catch { return true; }
    }
}
