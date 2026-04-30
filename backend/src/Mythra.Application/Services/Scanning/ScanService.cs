using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Background;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Application.Services.Notifications;
using Mythra.Domain.Common;
using Mythra.Domain.Notifications;

namespace Mythra.Application.Services.Scanning;

public sealed class ScanService(
    ILibraryRepository libraries,
    IMediaScannerRegistry scanners,
    INotificationService notifications,
    IBackgroundJobQueue jobs,
    IUnitOfWork uow,
    ILogger<ScanService> log) : IScanService
{
    public async Task<Result<ScanResult>> RunAsync(Guid libraryId, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(libraryId, ct);
        if (lib is null) return Error.NotFound("Library", libraryId);

        var scanner = scanners.Resolve(lib.Kind);
        if (scanner is null) return Error.Validation($"No scanner registered for kind {lib.Kind}.");

        var paths = lib.Folders.Where(f => f.IsActive).Select(f => f.Path).ToList();
        if (paths.Count == 0) return Error.Validation("Library has no active folders.");

        log.LogInformation("Starting scan for library {Id} ({Kind}) with {Count} folders", lib.Id, lib.Kind, paths.Count);
        var result = await scanner.ScanAsync(libraryId, paths, ct);
        lib.MarkScanned();
        foreach (var folder in lib.Folders.Where(f => f.IsActive)) folder.LastScannedAt = DateTimeOffset.UtcNow;
        await uow.SaveChangesAsync(ct);

        // Enqueue metadata enrichment for newly added items
        var newIds = result.NewItemIds ?? [];
        if (lib.AutoRefreshMetadata && newIds.Count > 0)
        {
            foreach (var id in newIds)
                await jobs.EnqueueAsync(new RefreshMetadataJob(Guid.NewGuid(), id, null), ct);
            log.LogInformation("Enqueued metadata enrichment for {Count} new items.", newIds.Count);
        }

        log.LogInformation("Scan finished: +{Added} ~{Updated} -{Removed} !{Failed} in {Elapsed}",
            result.Added, result.Updated, result.Removed, result.Failed, result.Elapsed);

        // Send scan-completed notification
        if (result.Added > 0 || result.Updated > 0)
        {
            var body = result.Added > 0
                ? $"+{result.Added} new item{(result.Added == 1 ? "" : "s")} in {lib.Name}."
                : $"{result.Updated} item{(result.Updated == 1 ? "" : "s")} updated in {lib.Name}.";

            await notifications.CreateAsync(Notification.Create(
                NotificationKind.ScanCompleted,
                title: $"Scan completed: {lib.Name}",
                body:  body,
                actionUrl: "/settings#libraries",
                payload: $"{{\"libraryId\":\"{lib.Id}\",\"added\":{result.Added},\"updated\":{result.Updated}}}"),
                ct);
        }

        return result;
    }
}
