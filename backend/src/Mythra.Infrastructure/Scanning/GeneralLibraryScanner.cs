using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Libraries;

namespace Mythra.Infrastructure.Scanning;

/// <summary>
/// Scanner for <see cref="LibraryKind.General"/> libraries.
/// Delegates to each specific scanner (injected as concrete types to avoid circular
/// <see cref="IEnumerable{IMediaScanner}"/> dependency).
/// </summary>
public sealed class GeneralLibraryScanner(
    VideoLibraryScanner videoScanner,
    BookLibraryScanner bookScanner,
    MangaLibraryScanner mangaScanner,
    AudioLibraryScanner audioScanner,
    ILogger<GeneralLibraryScanner> log) : IMediaScanner
{
    public LibraryKind Kind => LibraryKind.General;

    public async Task<ScanResult> ScanAsync(Guid libraryId, IReadOnlyList<string> rootPaths, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var totalAdded = 0; var totalUpdated = 0; var totalFailed = 0;
        var allErrors = new List<string>();
        var allNewIds = new List<Guid>();

        var subScanners = new (IMediaScanner Scanner, string Name)[]
        {
            (videoScanner, "Video"),
            (bookScanner,  "Book"),
            (mangaScanner, "Manga"),
            (audioScanner, "Audio"),
        };

        log.LogInformation("GeneralLibraryScanner running {Count} sub-scanners on library {Id}", subScanners.Length, libraryId);

        foreach (var (scanner, name) in subScanners)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var result = await scanner.ScanAsync(libraryId, rootPaths, ct);
                totalAdded   += result.Added;
                totalUpdated += result.Updated;
                totalFailed  += result.Failed;
                allErrors.AddRange(result.Errors);
                if (result.NewItemIds is not null) allNewIds.AddRange(result.NewItemIds);
                log.LogDebug("GeneralScanner/{Name}: +{A} ~{U} !{F}", name, result.Added, result.Updated, result.Failed);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "GeneralLibraryScanner: sub-scanner {Name} threw an exception.", name);
                allErrors.Add($"{name} scanner error: {ex.Message}");
            }
        }

        return new ScanResult(totalAdded, totalUpdated, 0, totalFailed, sw.Elapsed, allErrors, allNewIds);
    }
}
