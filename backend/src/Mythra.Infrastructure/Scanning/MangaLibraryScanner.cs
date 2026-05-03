using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Files;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media.Manga;
using SharpCompress.Archives;

namespace Mythra.Infrastructure.Scanning;

public sealed partial class MangaLibraryScanner(
    IFileSystem fs,
    IMangaRepository mangas,
    IUnitOfWork uow,
    ILogger<MangaLibraryScanner> log) : IMediaScanner
{
    private static readonly string[] ArchiveExtensions = [".cbz", ".cbr", ".zip", ".rar"];
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".avif", ".gif"];

    public LibraryKind Kind => LibraryKind.Manga;

    public async Task<ScanResult> ScanAsync(Guid libraryId, IReadOnlyList<string> rootPaths, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var added = 0;
        var updated = 0;
        var failed = 0;
        var errors = new List<string>();
        var newItemIds = new List<Guid>();
        var extensions = ArchiveExtensions;

        foreach (var root in rootPaths)
        {
            if (!fs.DirectoryExists(root))
            {
                errors.Add($"Folder not found: {root}");
                continue;
            }

            // Collect directories to scan: subdirectories + root itself (for root-level archives)
            var dirsToScan = new List<string>();
            dirsToScan.AddRange(Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly));

            // Check for root-level manga archives and treat root as a "series" if found
            var rootArchives = Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();
            if (rootArchives.Count > 0)
                dirsToScan.Insert(0, root);

            foreach (var seriesDir in dirsToScan)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var seriesName = new DirectoryInfo(seriesDir).Name;
                    var existing = await mangas.GetByRootPathAsync(seriesDir, ct);
                    var manga = existing ?? new MangaItem
                    {
                        LibraryId = libraryId,
                        Title = seriesName,
                        RootPath = seriesDir,
                    };
                    var isNew = existing is null;

                    // For root, only get top-level files; for subdirs, get all recursively
                    var searchOption = seriesDir == root ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
                    var chapterFiles = Directory.EnumerateFiles(seriesDir, "*", searchOption)
                        .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => f)
                        .ToList();

                    var chapters = new List<MangaChapter>();
                    foreach (var archive in chapterFiles)
                    {
                        var (vol, chap) = ParseChapter(Path.GetFileName(archive));
                        var pageCount = CountPagesSafely(archive);
                        chapters.Add(new MangaChapter
                        {
                            MangaItemId = manga.Id,
                            ArchivePath = archive,
                            ArchiveFormat = Path.GetExtension(archive).TrimStart('.').ToLowerInvariant(),
                            VolumeNumber = vol,
                            ChapterNumber = chap,
                            PageCount = pageCount,
                            Title = $"Chapter {chap:0.##}",
                        });
                    }
                    manga.Chapters = chapters;
                    manga.TotalChapters = chapters.Count;
                    manga.LastScannedAt = DateTimeOffset.UtcNow;
                    if (isNew)
                    {
                        await mangas.AddAsync(manga, ct);
                        newItemIds.Add(manga.Id);
                        added++;
                    }
                    else updated++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{seriesDir}: {ex.Message}");
                    log.LogWarning(ex, "Manga scan failed for {Path}", seriesDir);
                }
            }
        }

        await uow.SaveChangesAsync(ct);
        sw.Stop();
        return new ScanResult(added, updated, Removed: 0, failed, sw.Elapsed, errors, newItemIds);
    }

    private static (int? vol, double chap) ParseChapter(string filename)
    {
        var name = Path.GetFileNameWithoutExtension(filename);
        var volMatch = VolumeRegex().Match(name);
        int? vol = volMatch.Success ? int.Parse(volMatch.Groups[1].Value) : null;
        var chapMatch = ChapterRegex().Match(name);
        var chap = chapMatch.Success && double.TryParse(chapMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out var c) ? c : 1.0;
        return (vol, chap);
    }

    private static int CountPagesSafely(string archivePath)
    {
        try
        {
            using var archive = ArchiveFactory.Open(archivePath);
            return archive.Entries.Count(e => !e.IsDirectory && ImageExtensions.Contains(Path.GetExtension(e.Key ?? "").ToLowerInvariant()));
        }
        catch
        {
            return 0;
        }
    }

    [GeneratedRegex(@"\bv(?:ol(?:ume)?)?[ ._]*(\d{1,3})", RegexOptions.IgnoreCase)]
    private static partial Regex VolumeRegex();

    [GeneratedRegex(@"\b(?:c|ch|chapter)?[ ._]*(\d{1,4}(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex ChapterRegex();
}
