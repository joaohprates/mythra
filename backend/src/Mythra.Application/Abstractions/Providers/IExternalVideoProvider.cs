using Mythra.Domain.Media;

namespace Mythra.Application.Abstractions.Providers;

/// <summary>How the stream URL is meant to be consumed by the client.</summary>
public enum ExternalStreamKind
{
    /// <summary>An iframe-embeddable URL (e.g. Vidsrc).</summary>
    IframeEmbed = 1,

    /// <summary>An HLS .m3u8 manifest URL (e.g. Consumet/GogoAnime).</summary>
    HlsManifest = 2,

    /// <summary>A direct MP4 / video-file download URL (e.g. Archive.org).</summary>
    DirectMp4 = 3,
}

/// <summary>All identifiers Mythra knows about for a given media item.</summary>
public sealed record ExternalStreamRequest(
    Guid      MediaItemId,
    string    Title,
    MediaKind Kind,
    string?   ImdbId       = null,
    string?   TmdbId       = null,
    string?   AniListId    = null,
    string?   ArchiveOrgId = null,
    int?      Season       = null,
    int?      Episode      = null);

/// <summary>A playable URL resolved by a provider.</summary>
public sealed record ExternalStreamResult(
    string             ProviderName,
    ExternalStreamKind StreamKind,
    string             Url,
    string?            RefererUrl       = null,
    IReadOnlyDictionary<string, string>? Headers = null,
    int?               ExpiresInSeconds = null);

public interface IExternalVideoProvider
{
    string Name { get; }

    /// <summary>Lower value = tried first by the fallback orchestrator.</summary>
    int Priority { get; }

    bool Supports(MediaKind kind);

    Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default);
}
