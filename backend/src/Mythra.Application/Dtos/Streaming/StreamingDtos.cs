using Mythra.Domain.Streaming;

namespace Mythra.Application.Dtos.Streaming;

public sealed record StartStreamRequest(
    Guid MediaItemId,
    int? PreferredWidth,
    int? PreferredHeight,
    long? PreferredBitrate,
    int? AudioStreamIndex,
    int? SubtitleStreamIndex,
    bool ForceTranscode = false,
    bool BurnSubtitles = false);

public sealed record StreamSessionDto(
    Guid SessionId,
    string SessionToken,
    StreamMode Mode,
    StreamState State,
    string PlaylistUrl,
    int? Width,
    int? Height,
    long? Bitrate);

public sealed record StreamProbeDto(
    string Container,
    TimeSpan Duration,
    long FileSizeBytes,
    long? OverallBitrate,
    int VideoStreamCount,
    int AudioStreamCount,
    int SubtitleStreamCount);
