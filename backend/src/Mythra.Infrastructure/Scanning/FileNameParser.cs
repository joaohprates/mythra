using System.Text.RegularExpressions;

namespace Mythra.Infrastructure.Scanning;

public sealed record ParsedVideoName(
    string Title,
    int? Year,
    int? Season,
    int? Episode,
    int? AbsoluteEpisode,
    string? EditionTag,
    bool IsAnime);

public static partial class FileNameParser
{
    private static readonly string[] CleanupTokens =
    [
        "1080p", "720p", "2160p", "480p", "4k", "uhd", "hdr", "dovi", "dv",
        "x264", "x265", "h264", "h265", "hevc", "av1", "10bit", "8bit",
        "bluray", "brrip", "bdrip", "webrip", "web-dl", "webdl", "hdtv", "dvdrip",
        "ddp5.1", "ddp7.1", "dts", "dts-hd", "dts-x", "atmos", "aac", "ac3", "flac",
        "remux", "proper", "repack", "extended", "directors-cut", "uncut",
        "yify", "yts", "rarbg", "psa", "ettv", "eztv", "racetop", "subsplease",
    ];

    public static ParsedVideoName ParseVideo(string filename)
    {
        var raw = Path.GetFileNameWithoutExtension(filename).Replace('_', ' ').Replace('.', ' ');
        var lower = raw.ToLowerInvariant();
        var isAnime = AnimeRegex().IsMatch(lower) || lower.Contains("[subsplease]") || lower.Contains("[horriblesubs]");

        var seasonMatch = SeasonEpisodeRegex().Match(raw);
        if (seasonMatch.Success)
        {
            var title = CleanTitle(raw[..seasonMatch.Index]);
            return new ParsedVideoName(
                Title: title,
                Year: ExtractYear(raw),
                Season: int.Parse(seasonMatch.Groups[1].Value),
                Episode: int.Parse(seasonMatch.Groups[2].Value),
                AbsoluteEpisode: null,
                EditionTag: ExtractEdition(lower),
                IsAnime: isAnime);
        }

        var animeMatch = AnimeEpisodeRegex().Match(raw);
        if (animeMatch.Success && isAnime)
        {
            var title = CleanTitle(raw[..animeMatch.Index]);
            return new ParsedVideoName(
                Title: title,
                Year: ExtractYear(raw),
                Season: null,
                Episode: null,
                AbsoluteEpisode: int.Parse(animeMatch.Groups[1].Value),
                EditionTag: ExtractEdition(lower),
                IsAnime: true);
        }

        var year = ExtractYear(raw);
        var fullTitle = CleanTitle(raw);
        if (year.HasValue)
        {
            var idx = fullTitle.IndexOf(year.Value.ToString(), StringComparison.Ordinal);
            if (idx > 0) fullTitle = fullTitle[..idx].TrimEnd(' ', '-', '(');
        }
        return new ParsedVideoName(fullTitle, year, null, null, null, ExtractEdition(lower), isAnime);
    }

    private static string CleanTitle(string raw)
    {
        var cleaned = raw;
        foreach (var token in CleanupTokens)
        {
            cleaned = Regex.Replace(cleaned, $@"\b{Regex.Escape(token)}\b", "", RegexOptions.IgnoreCase);
        }
        cleaned = Regex.Replace(cleaned, @"\[[^\]]*\]", " ");
        cleaned = Regex.Replace(cleaned, @"\([^)]*\)", " ");
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim(' ', '-', '_', '.');
        return cleaned;
    }

    private static int? ExtractYear(string text)
    {
        var match = YearRegex().Match(text);
        return match.Success && int.TryParse(match.Groups[1].Value, out var y) && y is >= 1900 and <= 2100 ? y : null;
    }

    private static string? ExtractEdition(string lower)
    {
        if (lower.Contains("director")) return "directors-cut";
        if (lower.Contains("extended")) return "extended";
        if (lower.Contains("uncut")) return "uncut";
        if (lower.Contains("remastered")) return "remastered";
        if (lower.Contains("imax")) return "imax";
        return null;
    }

    [GeneratedRegex(@"[Ss](\d{1,2})\s?[Ee](\d{1,3})")]
    private static partial Regex SeasonEpisodeRegex();

    [GeneratedRegex(@"\b(?:ep|episode|#)?\s?-?\s?(\d{2,3})\b(?!\s*p)")]
    private static partial Regex AnimeEpisodeRegex();

    [GeneratedRegex(@"\b(19\d{2}|20\d{2})\b")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"\b(anime|animated|sub|dub|raw)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AnimeRegex();
}
