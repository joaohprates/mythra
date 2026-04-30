using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Files;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media.Audio;

namespace Mythra.Infrastructure.Scanning;

public sealed class AudioLibraryScanner(
    IFileSystem fs,
    IAudioRepository audios,
    IUnitOfWork uow,
    ILogger<AudioLibraryScanner> log) : IMediaScanner
{
    private static readonly string[] AudioExtensions = [".mp3", ".m4a", ".m4b", ".flac", ".ogg", ".opus", ".wav", ".aac"];

    public LibraryKind Kind => LibraryKind.Audiobook;

    public async Task<ScanResult> ScanAsync(Guid libraryId, IReadOnlyList<string> rootPaths, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var added = 0;
        var updated = 0;
        var failed = 0;
        var errors = new List<string>();
        var newItemIds = new List<Guid>();

        foreach (var root in rootPaths)
        {
            if (!fs.DirectoryExists(root))
            {
                errors.Add($"Folder not found: {root}");
                continue;
            }

            foreach (var albumDir in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var audioFiles = Directory.EnumerateFiles(albumDir, "*", SearchOption.AllDirectories)
                        .Where(f => AudioExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        .OrderBy(f => f)
                        .ToList();
                    if (audioFiles.Count == 0) continue;

                    var book = new AudioItem
                    {
                        LibraryId = libraryId,
                        Title = new DirectoryInfo(albumDir).Name,
                        AudioKind = AudioKind.Audiobook,
                        RootPath = albumDir,
                        LastScannedAt = DateTimeOffset.UtcNow,
                    };

                    var totalDuration = TimeSpan.Zero;
                    var order = 0;
                    foreach (var f in audioFiles)
                    {
                        try
                        {
                            using var tag = TagLib.File.Create(f);
                            if (order == 0)
                            {
                                if (!string.IsNullOrEmpty(tag.Tag.Title) && audioFiles.Count == 1)
                                    book.Title = tag.Tag.Album ?? tag.Tag.Title ?? book.Title;
                                else if (!string.IsNullOrEmpty(tag.Tag.Album))
                                    book.Title = tag.Tag.Album;
                                book.Author = tag.Tag.FirstAlbumArtist ?? tag.Tag.FirstPerformer;
                                book.Narrator = tag.Tag.FirstComposer;
                            }
                            book.Chapters.Add(new AudioChapter
                            {
                                AudioItemId = book.Id,
                                Order = order++,
                                Title = tag.Tag.Title ?? Path.GetFileNameWithoutExtension(f),
                                FilePath = f,
                                Start = totalDuration,
                                Duration = tag.Properties.Duration,
                                Codec = tag.Properties.Description?.Split(',').FirstOrDefault()?.Trim(),
                                Bitrate = tag.Properties.AudioBitrate,
                            });
                            totalDuration += tag.Properties.Duration;
                        }
                        catch (Exception ex)
                        {
                            log.LogTrace(ex, "Tag read failed for {Path}", f);
                        }
                    }
                    book.Duration = totalDuration;
                    await audios.AddAsync(book, ct);
                    newItemIds.Add(book.Id);
                    added++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{albumDir}: {ex.Message}");
                    log.LogWarning(ex, "Audio scan failed for {Path}", albumDir);
                }
            }
        }

        await uow.SaveChangesAsync(ct);
        sw.Stop();
        return new ScanResult(added, updated, Removed: 0, failed, sw.Elapsed, errors, newItemIds);
    }
}
