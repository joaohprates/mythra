using Mythra.Domain.Progress;

namespace Mythra.Application.Abstractions.Persistence;

public interface IPlaybackProgressRepository : IRepository<PlaybackProgress>
{
    Task<PlaybackProgress?> GetAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<IReadOnlyList<PlaybackProgress>> ContinueWatchingAsync(Guid profileId, int take, CancellationToken ct = default);
}

public interface IReadingProgressRepository : IRepository<ReadingProgress>
{
    Task<ReadingProgress?> GetAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<IReadOnlyList<ReadingProgress>> ContinueReadingAsync(Guid profileId, int take, CancellationToken ct = default);
}

public interface IBookmarkRepository : IRepository<Bookmark>
{
    Task<IReadOnlyList<Bookmark>> ListAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
}

public interface IHighlightRepository : IRepository<Highlight>
{
    Task<IReadOnlyList<Highlight>> ListAsync(Guid profileId, Guid mediaItemId, CancellationToken ct = default);
}
