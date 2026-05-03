using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Progress;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class PlaybackProgressRepository(MythraDbContext db) : EfRepository<PlaybackProgress>(db), IPlaybackProgressRepository
{
    public Task<PlaybackProgress?> GetAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(p => p.ProfileId == profileId && p.MediaItemId == mediaItemId, ct);

    public async Task<IReadOnlyList<PlaybackProgress>> ContinueWatchingAsync(Guid profileId, int take, CancellationToken ct = default) =>
        await Set.Where(p => p.ProfileId == profileId && !p.IsCompleted)
                 .OrderByDescending(p => p.LastWatchedAt)
                 .Take(Math.Clamp(take, 1, 100))
                 .ToListAsync(ct);

    public async Task<IReadOnlyList<PlaybackProgress>> GetAllForProfileAsync(Guid profileId, DateTimeOffset? since = null, CancellationToken ct = default)
    {
        var q = Set.Where(p => p.ProfileId == profileId);
        if (since.HasValue) q = q.Where(p => p.LastWatchedAt >= since.Value);
        return await q.OrderByDescending(p => p.LastWatchedAt).ToListAsync(ct);
    }
}

public sealed class ReadingProgressRepository(MythraDbContext db) : EfRepository<ReadingProgress>(db), IReadingProgressRepository
{
    public Task<ReadingProgress?> GetAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(r => r.ProfileId == profileId && r.MediaItemId == mediaItemId, ct);

    public async Task<IReadOnlyList<ReadingProgress>> ContinueReadingAsync(Guid profileId, int take, CancellationToken ct = default) =>
        await Set.Where(r => r.ProfileId == profileId && !r.IsCompleted)
                 .OrderByDescending(r => r.LastReadAt)
                 .Take(Math.Clamp(take, 1, 100))
                 .ToListAsync(ct);

    public async Task<IReadOnlyList<ReadingProgress>> GetAllForProfileAsync(Guid profileId, DateTimeOffset? since = null, CancellationToken ct = default)
    {
        var q = Set.Where(r => r.ProfileId == profileId);
        if (since.HasValue) q = q.Where(r => r.LastReadAt >= since.Value);
        return await q.OrderByDescending(r => r.LastReadAt).ToListAsync(ct);
    }
}

public sealed class BookmarkRepository(MythraDbContext db) : EfRepository<Bookmark>(db), IBookmarkRepository
{
    public async Task<IReadOnlyList<Bookmark>> ListAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default) =>
        await Set.Where(b => b.ProfileId == profileId && b.MediaItemId == mediaItemId)
                 .OrderBy(b => b.CreatedAt)
                 .ToListAsync(ct);
}

public sealed class HighlightRepository(MythraDbContext db) : EfRepository<Highlight>(db), IHighlightRepository
{
    public async Task<IReadOnlyList<Highlight>> ListAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default) =>
        await Set.Where(h => h.ProfileId == profileId && h.MediaItemId == mediaItemId)
                 .OrderBy(h => h.CreatedAt)
                 .ToListAsync(ct);
}
