using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Streaming;
using Mythra.Application.Dtos.Streaming;
using Mythra.Domain.Common;
using Mythra.Domain.Streaming;

namespace Mythra.Application.Services.Streaming;

public sealed class StreamingService(
    IVideoRepository videos,
    IStreamSessionRepository sessions,
    IMediaProbe probe,
    ITranscoder transcoder,
    IUnitOfWork uow,
    ILogger<StreamingService> log) : IStreamingService
{
    private const int DirectPlaySafeBitrate = 8_000_000;

    public async Task<Result<StreamSessionDto>> StartAsync(Guid userId, Guid profileId, StartStreamRequest req, string? userAgent, string? ip, CancellationToken ct = default)
    {
        var video = await videos.GetByIdAsync(req.MediaItemId, ct);
        if (video is null) return Error.NotFound("VideoItem", req.MediaItemId);
        if (string.IsNullOrWhiteSpace(video.FilePath))
            return Error.NotFound("LocalFile", "No local file. Install a streaming addon to play this item.");

        var mode = DecideMode(video, req);
        var session = new StreamSession
        {
            UserId = userId,
            ProfileId = profileId,
            VideoItemId = video.Id,
            Mode = mode,
            UserAgent = userAgent,
            IpAddress = ip,
            VideoCodec = video.VideoCodec,
            AudioCodec = video.AudioCodec,
            Width = req.PreferredWidth ?? video.Width,
            Height = req.PreferredHeight ?? video.Height,
            Bitrate = req.PreferredBitrate ?? video.Bitrate,
        };

        await sessions.AddAsync(session, ct);
        await uow.SaveChangesAsync(ct);

        var outputDir = Path.Combine(Path.GetTempPath(), "mythra", "transcode", session.SessionToken);
        Directory.CreateDirectory(outputDir);

        var transcodeReq = new TranscodeRequest(
            video.FilePath!,
            outputDir,
            req.PreferredWidth,
            req.PreferredHeight,
            req.PreferredBitrate,
            mode == StreamMode.Transcode ? "h264" : null,
            mode == StreamMode.Transcode ? "aac" : null,
            req.AudioStreamIndex,
            req.SubtitleStreamIndex,
            req.BurnSubtitles);

        try
        {
            var output = await transcoder.StartHlsAsync(transcodeReq, ct);
            session.MarkReady(output.PlaylistPath);
            await uow.SaveChangesAsync(ct);

            var playlistUrl = $"/api/v1/stream/{session.SessionToken}/playlist.m3u8";
            log.LogInformation("Started stream session {Token} ({Mode}) for {VideoId}", session.SessionToken, mode, video.Id);
            return new StreamSessionDto(session.Id, session.SessionToken, mode, session.State, playlistUrl, session.Width, session.Height, session.Bitrate);
        }
        catch (Exception ex)
        {
            session.Fail(ex.Message);
            await uow.SaveChangesAsync(ct);
            log.LogError(ex, "Failed to start stream session for {VideoId}", video.Id);
            return Error.Internal($"Failed to start stream: {ex.Message}");
        }
    }

    public async Task<Result> StopAsync(string sessionToken, CancellationToken ct = default)
    {
        var session = await sessions.GetByTokenAsync(sessionToken, ct);
        if (session is null) return Error.NotFound("StreamSession", sessionToken);
        if (session.PlaylistPath is not null)
            await transcoder.StopAsync(session.PlaylistPath, ct);
        session.End();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<StreamProbeDto?>> ProbeAsync(Guid videoItemId, CancellationToken ct = default)
    {
        var video = await videos.GetByIdAsync(videoItemId, ct);
        if (video is null || string.IsNullOrWhiteSpace(video.FilePath))
            return Error.NotFound("VideoItem", videoItemId);
        var probed = await probe.ProbeAsync(video.FilePath, ct);
        if (probed is null) return Result<StreamProbeDto?>.Success(null);
        return new StreamProbeDto(
            probed.Container,
            probed.Duration,
            probed.FileSizeBytes,
            probed.OverallBitrate,
            probed.VideoStreams.Count,
            probed.AudioStreams.Count,
            probed.SubtitleStreams.Count);
    }

    private static StreamMode DecideMode(Domain.Media.Video.VideoItem video, StartStreamRequest req)
    {
        if (req.ForceTranscode || req.BurnSubtitles) return StreamMode.Transcode;
        if (video.VideoCodec is "h264" or "hevc" && video.AudioCodec is "aac")
        {
            if (video.Bitrate is null || video.Bitrate <= DirectPlaySafeBitrate) return StreamMode.DirectPlay;
            return StreamMode.Remux;
        }
        return StreamMode.Transcode;
    }
}
