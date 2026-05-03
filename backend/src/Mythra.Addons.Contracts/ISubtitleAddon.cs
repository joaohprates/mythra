using Mythra.Addons.Contracts.Models;

namespace Mythra.Addons.Contracts;

/// <summary>
/// Addon that finds and provides subtitle files for media items.
/// </summary>
public interface ISubtitleAddon : IAddon
{
    /// <summary>
    /// Search for available subtitles. Return empty list — never throw — when none found.
    /// </summary>
    Task<IReadOnlyList<AddonSubtitleResult>> SearchSubtitlesAsync(
        AddonSubtitleRequest request,
        CancellationToken ct = default);
}

public sealed record AddonSubtitleRequest(
    string MediaTitle,
    string? ImdbId,
    string? Language,
    int? Season,
    int? Episode);

public sealed record AddonSubtitleResult(
    string Title,
    string Language,
    string DownloadUrl,
    string? Format = null,
    double? Rating = null);
