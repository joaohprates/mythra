namespace Mythra.Application.Abstractions.Streaming;

public sealed record TranscodeRequest(
    string SourcePath,
    string OutputDirectory,
    int? TargetWidth,
    int? TargetHeight,
    long? TargetBitrate,
    string? VideoCodec,
    string? AudioCodec,
    int? AudioStreamIndex,
    int? SubtitleStreamIndex,
    bool BurnSubtitles = false,
    int SegmentDurationSeconds = 6);

public sealed record TranscodeOutput(string PlaylistPath, string SegmentPattern);

public interface ITranscoder
{
    Task<TranscodeOutput> StartHlsAsync(TranscodeRequest request, CancellationToken ct = default);
    Task<bool> StopAsync(string playlistPath, CancellationToken ct = default);
    Task<byte[]?> ExtractThumbnailAsync(string sourcePath, TimeSpan position, int width, CancellationToken ct = default);
}
