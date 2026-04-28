using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Streaming;

namespace Mythra.Infrastructure.Streaming;

public sealed class FfmpegMediaProbe(IOptions<FfmpegOptions> opts, ILogger<FfmpegMediaProbe> log) : IMediaProbe
{
    private readonly FfmpegOptions _opts = opts.Value;

    public async Task<MediaProbeResult?> ProbeAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            log.LogWarning("Probe failed: file not found {Path}", filePath);
            return null;
        }

        var args = $"-v error -print_format json -show_format -show_streams \"{filePath}\"";
        var psi = new ProcessStartInfo
        {
            FileName = _opts.FfprobePath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            using var process = Process.Start(psi)!;
            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_opts.ProbeTimeoutSeconds));
            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode != 0)
            {
                log.LogError("ffprobe failed for {Path}: {Stderr}", filePath, stderr);
                return null;
            }

            using var doc = JsonDocument.Parse(stdout);
            var root = doc.RootElement;
            return Parse(filePath, root);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Probe exception for {Path}", filePath);
            return null;
        }
    }

    private static MediaProbeResult Parse(string filePath, JsonElement root)
    {
        var format = root.GetProperty("format");
        var container = format.TryGetProperty("format_name", out var f) ? f.GetString() ?? "unknown" : "unknown";
        var duration = format.TryGetProperty("duration", out var d) && double.TryParse(d.GetString(), out var ds)
            ? TimeSpan.FromSeconds(ds) : TimeSpan.Zero;
        var size = format.TryGetProperty("size", out var s) && long.TryParse(s.GetString(), out var sz) ? sz : new FileInfo(filePath).Length;
        var bitrate = format.TryGetProperty("bit_rate", out var br) && long.TryParse(br.GetString(), out var bbr) ? (long?)bbr : null;

        var videos = new List<VideoStreamInfo>();
        var audios = new List<AudioStreamInfo>();
        var subs = new List<SubtitleStreamInfo>();

        foreach (var stream in root.GetProperty("streams").EnumerateArray())
        {
            var index = stream.GetProperty("index").GetInt32();
            var codec = stream.GetProperty("codec_name").GetString() ?? "unknown";
            var type = stream.GetProperty("codec_type").GetString();

            switch (type)
            {
                case "video":
                    videos.Add(new VideoStreamInfo(
                        index, codec,
                        stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0,
                        stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0,
                        ParseFrameRate(stream),
                        stream.TryGetProperty("bit_rate", out var vbr) && long.TryParse(vbr.GetString(), out var vb) ? vb : null,
                        stream.TryGetProperty("pix_fmt", out var px) ? px.GetString() : null,
                        stream.TryGetProperty("profile", out var pr) ? pr.GetString() : null,
                        stream.TryGetProperty("level", out var lvl) && lvl.ValueKind == JsonValueKind.Number ? lvl.GetInt32() : null));
                    break;
                case "audio":
                    var tags = stream.TryGetProperty("tags", out var aTags) ? aTags : default;
                    audios.Add(new AudioStreamInfo(
                        index, codec,
                        stream.TryGetProperty("channels", out var ch) ? ch.GetInt32() : 2,
                        stream.TryGetProperty("channel_layout", out var cl) ? cl.GetString() ?? "stereo" : "stereo",
                        stream.TryGetProperty("sample_rate", out var sr) && int.TryParse(sr.GetString(), out var sri) ? sri : 0,
                        stream.TryGetProperty("bit_rate", out var ar) && long.TryParse(ar.GetString(), out var ab) ? ab : null,
                        tags.ValueKind != JsonValueKind.Undefined && tags.TryGetProperty("language", out var lng) ? lng.GetString() : null,
                        tags.ValueKind != JsonValueKind.Undefined && tags.TryGetProperty("title", out var ttl) ? ttl.GetString() : null,
                        IsDefault(stream)));
                    break;
                case "subtitle":
                    var sTags = stream.TryGetProperty("tags", out var subTags) ? subTags : default;
                    subs.Add(new SubtitleStreamInfo(
                        index, codec,
                        sTags.ValueKind != JsonValueKind.Undefined && sTags.TryGetProperty("language", out var slng) ? slng.GetString() : null,
                        sTags.ValueKind != JsonValueKind.Undefined && sTags.TryGetProperty("title", out var sttl) ? sttl.GetString() : null,
                        IsDefault(stream),
                        IsForced(stream)));
                    break;
            }
        }

        return new MediaProbeResult(container, duration, size, bitrate, videos, audios, subs);
    }

    private static double ParseFrameRate(JsonElement stream)
    {
        if (!stream.TryGetProperty("r_frame_rate", out var fr)) return 0;
        var s = fr.GetString() ?? "0/1";
        var parts = s.Split('/');
        if (parts.Length == 2 && double.TryParse(parts[0], out var num) && double.TryParse(parts[1], out var den) && den > 0)
            return num / den;
        return 0;
    }

    private static bool IsDefault(JsonElement stream) =>
        stream.TryGetProperty("disposition", out var d) && d.TryGetProperty("default", out var df) && df.GetInt32() == 1;

    private static bool IsForced(JsonElement stream) =>
        stream.TryGetProperty("disposition", out var d) && d.TryGetProperty("forced", out var df) && df.GetInt32() == 1;
}
