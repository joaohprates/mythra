using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Streaming;

namespace Mythra.Infrastructure.Streaming;

public sealed class FfmpegTranscoder(IOptions<FfmpegOptions> opts, ILogger<FfmpegTranscoder> log) : ITranscoder, IAsyncDisposable
{
    private readonly FfmpegOptions _opts = opts.Value;
    private readonly ConcurrentDictionary<string, Process> _running = new();

    public Task<TranscodeOutput> StartHlsAsync(TranscodeRequest request, CancellationToken ct = default)
    {
        Directory.CreateDirectory(request.OutputDirectory);
        var playlistPath = Path.Combine(request.OutputDirectory, "index.m3u8");
        var segmentPattern = Path.Combine(request.OutputDirectory, "seg_%05d.ts");

        var args = BuildArgs(request, playlistPath, segmentPattern);
        log.LogInformation("Spawning ffmpeg: {Args}", args);

        var psi = new ProcessStartInfo
        {
            FileName = _opts.FfmpegPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data)) log.LogTrace("ffmpeg: {Line}", e.Data);
        };

        try
        {
            process.Start();
            process.BeginErrorReadLine();
            _running[playlistPath] = process;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to spawn ffmpeg.");
            throw;
        }

        return Task.FromResult(new TranscodeOutput(playlistPath, segmentPattern));
    }

    public Task<bool> StopAsync(string playlistPath, CancellationToken ct = default)
    {
        if (!_running.TryRemove(playlistPath, out var process)) return Task.FromResult(false);
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to stop ffmpeg for {Path}", playlistPath);
            return Task.FromResult(false);
        }
        finally { process.Dispose(); }
    }

    public async Task<byte[]?> ExtractThumbnailAsync(string sourcePath, TimeSpan position, int width, CancellationToken ct = default)
    {
        if (!File.Exists(sourcePath)) return null;
        var tempPath = Path.Combine(Path.GetTempPath(), $"mythra_thumb_{Guid.NewGuid():N}.jpg");
        var args = $"-y -ss {position.TotalSeconds:0.000} -i \"{sourcePath}\" -frames:v 1 -vf scale={width}:-1 -q:v 4 \"{tempPath}\"";
        var psi = new ProcessStartInfo
        {
            FileName = _opts.FfmpegPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };

        try
        {
            using var process = Process.Start(psi)!;
            await process.WaitForExitAsync(ct);
            if (process.ExitCode != 0 || !File.Exists(tempPath)) return null;
            var bytes = await File.ReadAllBytesAsync(tempPath, ct);
            return bytes;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Thumbnail extraction failed for {Path}", sourcePath);
            return null;
        }
        finally { try { File.Delete(tempPath); } catch { } }
    }

    private string BuildArgs(TranscodeRequest req, string playlistPath, string segmentPattern)
    {
        var seg = req.SegmentDurationSeconds > 0 ? req.SegmentDurationSeconds : _opts.DefaultSegmentSeconds;
        var transcodeVideo = !string.IsNullOrEmpty(req.VideoCodec);
        var transcodeAudio = !string.IsNullOrEmpty(req.AudioCodec);

        var sb = new System.Text.StringBuilder();
        sb.Append("-y -hide_banner -loglevel warning ");
        sb.Append($"-i \"{req.SourcePath}\" ");

        if (req.AudioStreamIndex.HasValue)
            sb.Append($"-map 0:v:0 -map 0:a:{req.AudioStreamIndex.Value} ");
        else
            sb.Append("-map 0:v:0 -map 0:a:0? ");

        if (transcodeVideo)
        {
            sb.Append($"-c:v {req.VideoCodec} -preset {_opts.DefaultPreset} -crf {_opts.DefaultCrf} ");
            if (req.TargetWidth.HasValue || req.TargetHeight.HasValue)
            {
                var w = req.TargetWidth?.ToString() ?? "-2";
                var h = req.TargetHeight?.ToString() ?? "-2";
                sb.Append($"-vf scale={w}:{h} ");
            }
            if (req.TargetBitrate.HasValue)
                sb.Append($"-b:v {req.TargetBitrate} -maxrate {req.TargetBitrate * 1.07:F0} -bufsize {req.TargetBitrate * 2:F0} ");
        }
        else
            sb.Append("-c:v copy ");

        sb.Append(transcodeAudio ? $"-c:a {req.AudioCodec} -b:a 192k -ac 2 " : "-c:a copy ");

        if (req.BurnSubtitles && req.SubtitleStreamIndex.HasValue)
            sb.Append($"-vf \"subtitles='{req.SourcePath}':si={req.SubtitleStreamIndex}\" ");

        sb.Append($"-f hls -hls_time {seg} -hls_list_size {_opts.DefaultListSize} ");
        sb.Append("-hls_flags independent_segments+delete_segments+omit_endlist ");
        sb.Append("-hls_segment_type mpegts ");
        sb.Append($"-hls_segment_filename \"{segmentPattern}\" ");
        sb.Append($"\"{playlistPath}\"");
        return sb.ToString();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var kvp in _running)
        {
            try
            {
                if (!kvp.Value.HasExited) kvp.Value.Kill(entireProcessTree: true);
                kvp.Value.Dispose();
            }
            catch { }
        }
        _running.Clear();
        await Task.CompletedTask;
    }
}
