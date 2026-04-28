using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Common;
using Mythra.Domain.Events;

namespace Mythra.Application.Services.Scanning;

public sealed class ScanService(
    ILibraryRepository libraries,
    IMediaScannerRegistry scanners,
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

        log.LogInformation("Scan finished: +{Added} ~{Updated} -{Removed} !{Failed} in {Elapsed}",
            result.Added, result.Updated, result.Removed, result.Failed, result.Elapsed);
        return result;
    }
}
