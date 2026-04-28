using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Files;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Application.Abstractions.Streaming;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media.Video;

namespace Mythra.Infrastructure.Scanning;

public sealed class VideoLibraryScanner(
    IFileSystem fs,
    IVideoRepository videos,
    IMediaProbe probe,
    IUnitOfWork uow,
    ILogger<VideoLibraryScanner> log) : IMediaScanner
{
    private static readonly string[] VideoExtensions =
        [".mp4", ".mkv", ".m4v", ".mov", ".avi", ".webm", ".ts", ".mpg", ".mpeg", ".wmv"];

    public LibraryKind Kind => LibraryKind.Video;

    public async Task<ScanResult> ScanAsync(Guid libraryId, IReadOnlyList<string> rootPaths, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var added = 0;
        var updated = 0;
        var failed = 0;
        var errors = new List<string>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in rootPaths)
        {
            if (!fs.DirectoryExists(root))
            {
                errors.Add($"Folder not found: {root}");
                continue;
            }

            foreach (var entry in fs.EnumerateFiles(root, "*", recursive: true))
            {
                ct.ThrowIfCancellationRequested();
                if (!VideoExtensions.Contains(Path.GetExtension(entry.Path).ToLowerInvariant())) continue;
                seenPaths.Add(entry.Path);

                try
                {
                    var existing = await videos.GetByPathAsync(entry.Path, ct);
                    var parsed = FileNameParser.ParseVideo(entry.Path);
                    var probeResult = await probe.ProbeAsync(entry.Path, ct);
                    var primaryVideo = probeResult?.VideoStreams.FirstOrDefault();
                    var primaryAudio = probeResult?.AudioStreams.FirstOrDefault(a => a.IsDefault) ?? probeResult?.AudioStreams.FirstOrDefault();

                    if (existing is null)
                    {
                        var item = new VideoItem
                        {
                            LibraryId = libraryId,
                            Title = string.IsNullOrEmpty(parsed.Title) ? Path.GetFileNameWithoutExtension(entry.Path) : parsed.Title,
                            FilePath = entry.Path,
                            FileSizeBytes = entry.Size,
                            VideoKind = parsed.Episode.HasValue ? VideoKind.Episode : (parsed.IsAnime ? VideoKind.Anime : VideoKind.Movie),
                            IsAnime = parsed.IsAnime,
                            SeasonNumber = parsed.Season,
                            EpisodeNumber = parsed.Episode,
                            AbsoluteEpisodeNumber = parsed.AbsoluteEpisode,
                            ReleaseDate = parsed.Year.HasValue ? new DateOnly(parsed.Year.Value, 1, 1) : null,
                        };
                        ApplyProbe(item, probeResult, primaryVideo, primaryAudio);
                        item.LastScannedAt = DateTimeOffset.UtcNow;
                        await videos.AddAsync(item, ct);
                        added++;
                    }
                    else
                    {
                        existing.FileSizeBytes = entry.Size;
                        ApplyProbe(existing, probeResult, primaryVideo, primaryAudio);
                        existing.LastScannedAt = DateTimeOffset.UtcNow;
                        existing.Touch();
                        updated++;
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{entry.Path}: {ex.Message}");
                    log.LogWarning(ex, "Scan failed for {Path}", entry.Path);
                }
            }
        }

        await uow.SaveChangesAsync(ct);
        sw.Stop();
        return new ScanResult(added, updated, Removed: 0, failed, sw.Elapsed, errors);
    }

    private static void ApplyProbe(VideoItem item, MediaProbeResult? probe, VideoStreamInfo? primaryVideo, AudioStreamInfo? primaryAudio)
    {
        if (probe is null) return;
        item.Container = probe.Container;
        item.Duration = probe.Duration;
        item.Bitrate = probe.OverallBitrate;
        if (primaryVideo is not null)
        {
            item.VideoCodec = primaryVideo.Codec;
            item.Width = primaryVideo.Width;
            item.Height = primaryVideo.Height;
            item.FrameRate = primaryVideo.FrameRate;
        }
        if (primaryAudio is not null)
        {
            item.AudioCodec = primaryAudio.Codec;
        }

        item.Subtitles = probe.SubtitleStreams.Select(s => new Subtitle
        {
            VideoItemId = item.Id,
            LanguageCode = s.Language ?? "und",
            DisplayName = s.Title,
            Format = s.Codec,
            StreamIndex = s.Index,
            Kind = SubtitleKind.Embedded,
            IsDefault = s.IsDefault,
            IsForced = s.IsForced,
        }).ToList();

        item.AudioTracks = probe.AudioStreams.Select(a => new AudioTrack
        {
            VideoItemId = item.Id,
            LanguageCode = a.Language ?? "und",
            DisplayName = a.Title,
            StreamIndex = a.Index,
            Codec = a.Codec,
            Channels = a.Channels,
            ChannelLayout = a.ChannelLayout,
            SampleRate = a.SampleRate,
            Bitrate = a.Bitrate,
            IsDefault = a.IsDefault,
        }).ToList();
    }
}
