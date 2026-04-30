using Mythra.Application.Abstractions.Metadata;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.Recommendations;

public sealed class RecommendationService(
    IPlaybackProgressRepository playbackRepo,
    IReadingProgressRepository readingRepo,
    IMediaItemRepository mediaRepo,
    IMetadataProviderRegistry metadataRegistry) : IRecommendationService
{
    public async Task<Result<IReadOnlyList<RecommendationItemDto>>> GetForProfileAsync(
        Guid profileId, int take = 20, CancellationToken ct = default)
    {
        // 1. Collect recently consumed item IDs
        var recentPlayback = await playbackRepo.ContinueWatchingAsync(profileId, 20, ct);
        var recentReading  = await readingRepo.ContinueReadingAsync(profileId, 20, ct);

        var consumedIds = recentPlayback.Select(p => p.MediaItemId)
            .Union(recentReading.Select(r => r.MediaItemId))
            .ToHashSet();

        // 2. Fetch details for consumed items to extract genres
        var consumedItems = await mediaRepo.ByIdsAsync(consumedIds, ct);

        // Genre frequency map
        var genreFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in consumedItems)
        {
            foreach (var g in item.Genres.Select(g => g.Name))
            {
                genreFreq[g] = genreFreq.GetValueOrDefault(g) + 1;
            }
        }

        // If no history, fall back to top-rated across all kinds
        if (genreFreq.Count == 0)
        {
            var topRated = await mediaRepo.SearchAsync(
                new MediaQuery(Skip: 0, Take: take, OrderBy: "rating"), ct);

            return topRated
                .Select(m => ToDto(m, "Top rated"))
                .ToList();
        }

        // 3. Top genres (max 3)
        var topGenres = genreFreq
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => kv.Key)
            .ToList();

        // 4. Fetch candidates for each top genre, merge, deduplicate, exclude consumed
        var candidates = new Dictionary<Guid, (MediaItem Item, string TopGenre, int Score)>();

        foreach (var genre in topGenres)
        {
            var hits = await mediaRepo.SearchAsync(
                new MediaQuery(Genre: genre, Skip: 0, Take: 40, OrderBy: "rating"), ct);

            foreach (var hit in hits.Where(h => !consumedIds.Contains(h.Id)))
            {
                if (candidates.TryGetValue(hit.Id, out var existing))
                {
                    // boost score if appears in multiple genre queries
                    candidates[hit.Id] = existing with { Score = existing.Score + 1 };
                }
                else
                {
                    // base score: genre freq weight
                    var score = genreFreq.GetValueOrDefault(genre);
                    candidates[hit.Id] = (hit, genre, score);
                }
            }
        }

        // 5. Sort by score desc, then by rating desc
        var results = candidates.Values
            .OrderByDescending(c => c.Score)
            .ThenByDescending(c => c.Item.Rating ?? 0)
            .Take(take)
            .Select(c => ToDto(c.Item, $"Because you like {c.TopGenre}"))
            .ToList();

        return results;
    }

    public async Task<Result<IReadOnlyList<ProviderHealthDto>>> GetProviderHealthAsync(CancellationToken ct = default)
    {
        var kinds = new[] { MediaKind.Video, MediaKind.Book, MediaKind.Manga, MediaKind.Audio };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<ProviderHealthDto>();

        foreach (var kind in kinds)
        {
            foreach (var provider in metadataRegistry.ProvidersFor(kind))
            {
                if (!seen.Add(provider.Name)) continue;

                var isHealthy = true;
                string? errorMsg = null;

                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

                    // Lightweight ping — search with a known short query
                    await provider.SearchAsync("test", kind, null, timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    isHealthy = false;
                    errorMsg = "Timeout after 5 s";
                }
                catch (Exception ex)
                {
                    isHealthy = false;
                    errorMsg = ex.Message.Length > 200 ? ex.Message[..200] + "…" : ex.Message;
                }

                results.Add(new ProviderHealthDto(
                    Name: provider.Name,
                    IsHealthy: isHealthy,
                    ErrorMessage: errorMsg,
                    CheckedAt: DateTimeOffset.UtcNow));
            }
        }

        return results;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static RecommendationItemDto ToDto(MediaItem m, string reason) =>
        new(
            Id:          m.Id,
            Kind:        m.Kind,
            Title:       m.Title,
            PosterPath:  m.PosterPath,
            BackdropPath: m.BackdropPath,
            Rating:      m.Rating,
            Year:        m.ReleaseDate?.Year,
            Genres:      m.Genres.Select(g => g.Name).ToList(),
            Reason:      reason);
}
