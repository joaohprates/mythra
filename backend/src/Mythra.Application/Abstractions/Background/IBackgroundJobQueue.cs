namespace Mythra.Application.Abstractions.Background;

public abstract record BackgroundJob(Guid JobId, string Kind);

public sealed record ScanLibraryJob(Guid JobId, Guid LibraryId) : BackgroundJob(JobId, "scan-library");

public sealed record RefreshMetadataJob(Guid JobId, Guid MediaItemId, string? PreferredProvider) : BackgroundJob(JobId, "refresh-metadata");

public sealed record GenerateThumbnailsJob(Guid JobId, Guid VideoItemId) : BackgroundJob(JobId, "generate-thumbnails");

public sealed record CleanupTranscodeCacheJob(Guid JobId) : BackgroundJob(JobId, "cleanup-transcode-cache");

public interface IBackgroundJobQueue
{
    ValueTask EnqueueAsync(BackgroundJob job, CancellationToken ct = default);
    IAsyncEnumerable<BackgroundJob> DequeueAllAsync(CancellationToken ct);
}
