using Mythra.Addons.Contracts.Models;

namespace Mythra.Addons.Contracts;

/// <summary>
/// Addon that resolves a playable stream URL for a given media item.
/// Multiple stream addons can be registered; the host picks the one with the
/// highest Priority that returns a non-null result.
/// </summary>
public interface IStreamSourceAddon : IAddon
{
    bool Supports(AddonMediaKind kind);

    /// <summary>Higher number = tried first.</summary>
    int Priority { get; }

    /// <summary>
    /// Resolve a stream URL. Return null — never throw — when unavailable.
    /// </summary>
    Task<AddonStreamResult?> GetStreamAsync(AddonStreamRequest request, CancellationToken ct = default);
}

public sealed record AddonStreamRequest(
    string MediaTitle,
    string? ImdbId,
    string? TmdbId,
    int? Season,
    int? Episode,
    AddonMediaKind Kind);

public sealed record AddonStreamResult(
    AddonStreamKind Kind,
    string Url,
    string? Language = null,
    string? Quality = null,
    IReadOnlyDictionary<string, string>? Headers = null);

public enum AddonStreamKind { IframeEmbed, HlsManifest, DirectMp4 }
