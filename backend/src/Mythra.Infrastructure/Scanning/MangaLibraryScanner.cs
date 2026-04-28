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

        foreach (var root in rootPaths)
        {
            if (!fs.DirectoryExists(root))
            {
                errors.Add($"Folder not found: {root}");
                continue;
            }

            foreach (var seriesDir in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var seriesName = new DirectoryInfo(seriesDir).Name;
                    var existing = (await mangas.GetByIdWithChaptersAsync(Guid.Empty, ct));
                    var manga = existing ?? new MangaItem
                    {
                        LibraryId = libraryId,
                        Title = seriesName,
                        RootPath = seriesDir,
                    };
                    var isNew = existing is null;

                    var chapterFiles = Directory.EnumerateFiles(seriesDir, "*", SearchOption.AllDirectories)
                        .Where(f => ArchiveExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
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
        return new ScanResult(added, updated, Removed: 0, failed, sw.Elapsed, errors);
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
