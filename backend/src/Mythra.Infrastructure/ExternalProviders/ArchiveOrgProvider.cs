using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Searches the Internet Archive for public-domain films and returns a direct MP4 URL.
/// If the media item carries an <c>ArchiveOrgId</c>, the search step is skipped.
/// </summary>
public sealed class ArchiveOrgProvider(
    HttpClient                         http,
    IOptions<ExternalProvidersOptions> options,
    ILogger<ArchiveOrgProvider>        logger) : IExternalVideoProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "Archive.org";
    public int    Priority => 30;

    public bool Supports(MediaKind kind) =>
        _opts.ArchiveOrgEnabled && kind is MediaKind.Video;

    public async Task<ExternalStreamResult?> GetStreamAsync(
        ExternalStreamRequest request,
        CancellationToken     ct = default)
    {
        if (!_opts.ArchiveOrgEnabled || request.Kind is not MediaKind.Video)
            return null;

        // Fast-path: a direct Archive.org identifier was supplied
        if (!string.IsNullOrWhiteSpace(request.ArchiveOrgId))
            return await GetByIdentifierAsync(request.ArchiveOrgId, ct);

        // Slow-path: search by title, restricting to Creative-Commons / public-domain items
        try
        {
            var q      = Uri.EscapeDataString($"title:({request.Title}) AND mediatype:(movies)");
            var search = await http.GetFromJsonAsync<ArchiveSearchWrapper>(
                $"{_opts.ArchiveOrgBaseUrl}/advancedsearch.php?q={q}&fl[]=identifier&rows=3&output=json",
                ct);

            var identifier = search?.Response?.Docs?.FirstOrDefault()?.Identifier;
            if (identifier is null) return null;

            return await GetByIdentifierAsync(identifier, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Archive.org] Search failed for '{Title}'", request.Title);
            return null;
        }
    }

    private async Task<ExternalStreamResult?> GetByIdentifierAsync(
        string archiveId, CancellationToken ct)
    {
        try
        {
            var meta = await http.GetFromJsonAsync<ArchiveMetadata>(
                $"{_opts.ArchiveOrgBaseUrl}/metadata/{archiveId}", ct);

            var file = meta?.Files?
                .Where(f =>
                    f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                    f.Name.EndsWith(".ogv", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (file is null) return null;

            return new ExternalStreamResult(
                ProviderName: Name,
                StreamKind:   ExternalStreamKind.DirectMp4,
                Url:          $"https://archive.org/download/{archiveId}/{file.Name}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Archive.org] Metadata fetch failed for '{Id}'", archiveId);
            return null;
        }
    }

    // ── JSON models ──────────────────────────────────────────────────────────

    private sealed record ArchiveSearchWrapper(
        [property: JsonPropertyName("response")] ArchiveSearchResponse? Response);

    private sealed record ArchiveSearchResponse(
        [property: JsonPropertyName("docs")] IReadOnlyList<ArchiveSearchDoc>? Docs);

    private sealed record ArchiveSearchDoc(
        [property: JsonPropertyName("identifier")] string Identifier);

    private sealed record ArchiveMetadata(
        [property: JsonPropertyName("files")] IReadOnlyList<ArchiveFile>? Files);

    private sealed record ArchiveFile(
        [property: JsonPropertyName("name")] string Name);
}
