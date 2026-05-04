using System.Text.RegularExpressions;

namespace Mythra.Infrastructure.ExternalProviders.Common;

/// <summary>
/// Centralised validation for the various external IDs Mythra forwards to upstream
/// providers (IMDb, TMDb, ISBN). Providers should call these before constructing URLs
/// to avoid sending mismatched id types (e.g. a TMDb numeric id to a /movie/{imdb} path).
/// </summary>
internal static class ExternalIdValidator
{
    private static readonly Regex ImdbRegex =
        new(@"^tt\d{6,10}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TmdbRegex =
        new(@"^\d{1,10}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsImdbId(string? s) =>
        !string.IsNullOrWhiteSpace(s) && ImdbRegex.IsMatch(s.Trim());

    public static bool IsTmdbId(string? s) =>
        !string.IsNullOrWhiteSpace(s) && TmdbRegex.IsMatch(s.Trim());

    /// <summary>True for ISBN-10 or ISBN-13 (digit-only form, no dashes).</summary>
    public static bool IsIsbn(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var trimmed = s.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
        if (trimmed.Length is not (10 or 13)) return false;
        foreach (var c in trimmed)
            if (!char.IsDigit(c)) return false;
        return true;
    }

    /// <summary>Returns the digits-only ISBN form, or null if input is not a valid ISBN.</summary>
    public static string? NormalizeIsbn(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var trimmed = s.Replace("-", string.Empty).Replace(" ", string.Empty).Trim();
        return IsIsbn(trimmed) ? trimmed : null;
    }
}
