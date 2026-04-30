using Mythra.Domain.Common;

namespace Mythra.Application.Services.ExternalProviders;

// ── DTOs returned by the service ─────────────────────────────────────────────

public sealed record ExternalVideoStreamDto(
    string  ProviderName,
    string  StreamKind,   // "IframeEmbed" | "HlsManifest" | "DirectMp4"
    string  Url,
    string? RefererUrl = null,
    IReadOnlyDictionary<string, string>? Headers = null);

public sealed record ExternalBookLinkDto(
    string  ProviderName,
    string  Format,       // "Epub" | "PlainText" | "Mp3" | "WebReader" | …
    string  Url,
    string? CoverUrl  = null,
    string? Language  = null,
    IReadOnlyList<string>? Authors = null);

// ── Service interface ─────────────────────────────────────────────────────────

public interface IExternalProviderService
{
    /// <summary>
    /// Tries each enabled video provider in priority order and returns the first
    /// successful stream URL for the specified media item.
    /// </summary>
    Task<Result<ExternalVideoStreamDto>> GetVideoStreamAsync(
        Guid              mediaItemId,
        int?              season  = null,
        int?              episode = null,
        CancellationToken ct      = default);

    /// <summary>
    /// Queries all enabled book / audio / manga providers and aggregates their links.
    /// </summary>
    Task<Result<IReadOnlyList<ExternalBookLinkDto>>> GetBookLinksAsync(
        Guid              mediaItemId,
        CancellationToken ct = default);
}
