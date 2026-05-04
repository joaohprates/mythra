using Microsoft.Extensions.Logging;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Wraps an IMetadataAddon (from Contracts) so it can participate in the existing
/// IMetadataProvider registry without any changes to the registry consumers.
/// </summary>
internal sealed class AddonMetadataBridge(IMetadataAddon addon, ILogger logger) : IMetadataProvider
{
    public string Name => addon.Id;

    public bool Supports(MediaKind kind) => addon.Supports(ToAddonKind(kind));

    public async Task<IReadOnlyList<MetadataSearchResult>> SearchAsync(
        string query, MediaKind kind, int? year, CancellationToken ct = default)
    {
        try
        {
            var results = await addon.SearchAsync(query, ToAddonKind(kind), year, ct);
            return results.Select(ToCore).ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Addon {Addon} search failed", addon.Id);
            return [];
        }
    }

    public async Task<MetadataSearchResult?> GetByIdAsync(
        string providerId, MediaKind kind, CancellationToken ct = default)
    {
        try
        {
            var result = await addon.GetByProviderIdAsync(providerId, ToAddonKind(kind), ct);
            return result is null ? null : ToCore(result);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Addon {Addon} GetById failed for {Id}", addon.Id, providerId);
            return null;
        }
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static AddonMediaKind ToAddonKind(MediaKind kind) => kind switch
    {
        MediaKind.Video => AddonMediaKind.Movie,
        MediaKind.Book  => AddonMediaKind.Book,
        MediaKind.Manga => AddonMediaKind.Manga,
        _               => AddonMediaKind.Movie,
    };

    private static MetadataSearchResult ToCore(AddonMetadataResult r) => new(
        ProviderId:    r.ProviderId,
        Title:         r.Title,
        OriginalTitle: r.OriginalTitle,
        Overview:      r.Overview,
        ReleaseDate:   r.ReleaseDate,
        PosterUrl:     r.PosterUrl,
        BackdropUrl:   r.BackdropUrl,
        Rating:        r.Rating,
        Genres:        r.Genres,
        ProviderIds:   r.ExternalIds,
        IsAdult:       r.IsAdult);
}
