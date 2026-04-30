using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.Media;

public sealed class MetadataEnrichmentService(
    IMediaItemRepository mediaItems,
    IMetadataProviderRegistry registry,
    IUnitOfWork uow,
    ILogger<MetadataEnrichmentService> log) : IMetadataEnrichmentService
{
    public async Task<Result> EnrichAsync(Guid mediaItemId, string? preferredProvider = null, CancellationToken ct = default)
    {
        var item = await mediaItems.GetByIdWithDetailsAsync(mediaItemId, ct);
        if (item is null) return Error.NotFound("MediaItem", mediaItemId);

        // Skip if metadata was refreshed recently (within 7 days) and not forced
        if (item.LastMetadataRefreshAt.HasValue &&
            item.LastMetadataRefreshAt.Value > DateTimeOffset.UtcNow.AddDays(-7))
        {
            log.LogDebug("Item {Id} metadata is fresh — skipping.", mediaItemId);
            return Result.Success();
        }

        IReadOnlyList<IMetadataProvider> providers;
        if (!string.IsNullOrWhiteSpace(preferredProvider))
        {
            var p = registry.GetByName(preferredProvider);
            providers = p is not null ? [p] : registry.ProvidersFor(item.Kind);
        }
        else
        {
            providers = registry.ProvidersFor(item.Kind);
        }

        if (providers.Count == 0)
        {
            log.LogDebug("No metadata provider for {Kind} — skipping item {Id}.", item.Kind, mediaItemId);
            return Result.Success();
        }

        foreach (var provider in providers)
        {
            try
            {
                MetadataSearchResult? meta = null;

                // Try to fetch by existing provider ID first
                var existingId = GetExistingProviderId(item, provider.Name);
                if (!string.IsNullOrEmpty(existingId))
                {
                    meta = await provider.GetByIdAsync(existingId, item.Kind, ct);
                }

                // Fall back to title search
                if (meta is null)
                {
                    var results = await provider.SearchAsync(item.Title, item.Kind, item.Year, ct);
                    meta = results.FirstOrDefault();
                }

                if (meta is null) continue;

                ApplyMetadata(item, meta, provider.Name);
                item.LastMetadataRefreshAt = DateTimeOffset.UtcNow;
                item.Touch();
                await uow.SaveChangesAsync(ct);

                log.LogInformation("Enriched item {Id} ({Title}) via {Provider}.", mediaItemId, item.Title, provider.Name);
                return Result.Success();
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Provider {Provider} failed for item {Id}.", provider.Name, mediaItemId);
            }
        }

        return Result.Success();
    }

    private static string? GetExistingProviderId(MediaItem item, string providerName) =>
        providerName.ToLowerInvariant() switch
        {
            "tmdb"        => item.ProviderTmdbId,
            "anilist"     => item.ProviderAnilistId,
            "googlebooks" => item.ProviderGoogleBooksId,
            "musicbrainz" => item.ProviderMusicbrainzId,
            _             => null,
        };

    private static void ApplyMetadata(MediaItem item, MetadataSearchResult meta, string providerName)
    {
        if (!string.IsNullOrWhiteSpace(meta.Title))
            item.Title = meta.Title;
        if (!string.IsNullOrWhiteSpace(meta.OriginalTitle))
            item.OriginalTitle = meta.OriginalTitle;
        if (!string.IsNullOrWhiteSpace(meta.Overview))
            item.Overview = meta.Overview;
        if (meta.ReleaseDate.HasValue && !item.ReleaseDate.HasValue)
            item.ReleaseDate = meta.ReleaseDate;
        if (meta.Rating.HasValue)
            item.Rating = meta.Rating;
        if (!string.IsNullOrWhiteSpace(meta.PosterUrl))
            item.PosterPath = meta.PosterUrl;
        if (!string.IsNullOrWhiteSpace(meta.BackdropUrl))
            item.BackdropPath = meta.BackdropUrl;

        // Apply provider IDs
        foreach (var (key, value) in meta.ProviderIds)
        {
            switch (key.ToLowerInvariant())
            {
                case "tmdb":        item.ProviderTmdbId        = value; break;
                case "imdb":        item.ProviderImdbId        = value; break;
                case "anilist":     item.ProviderAnilistId     = value; break;
                case "mal":         item.ProviderMalId         = value; break;
                case "musicbrainz": item.ProviderMusicbrainzId = value; break;
                case "google":      item.ProviderGoogleBooksId = value; break;
                case "mangadex":    item.ProviderMangaDexId    = value; break;
            }
        }

        // Add new genres (avoid duplicates by name)
        foreach (var genreName in meta.Genres)
        {
            if (!item.Genres.Any(g => string.Equals(g.Name, genreName, StringComparison.OrdinalIgnoreCase)))
                item.Genres.Add(new Genre(genreName));
        }
    }
}
