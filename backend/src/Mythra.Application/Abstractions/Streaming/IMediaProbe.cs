namespace Mythra.Application.Abstractions.Streaming;

public sealed record VideoStreamInfo(
    int Index,
    string Codec,
    int Width,
    int Height,
    double FrameRate,
    long? Bitrate,
    string? PixelFormat,
    string? Profile,
    int? Level);

public sealed record AudioStreamInfo(
    int Index,
    string Codec,
    int Channels,
    string ChannelLayout,
    int SampleRate,
    long? Bitrate,
    string? Language,
    string? Title,
    bool IsDefault);

public sealed record SubtitleStreamInfo(
    int Index,
    string Codec,
    string? Language,
    string? Title,
    bool IsDefault,
    bool IsForced);

public sealed record MediaProbeResult(
    string Container,
    TimeSpan Duration,
    long FileSizeBytes,
    long? OverallBitrate,
    IReadOnlyList<VideoStreamInfo> VideoStreams,
    IReadOnlyList<AudioStreamInfo> AudioStreams,
    IReadOnlyList<SubtitleStreamInfo> SubtitleStreams);

public interface IMediaProbe
{
    Task<MediaProbeResult?> ProbeAsync(string filePath, CancellationToken ct = default);
}
