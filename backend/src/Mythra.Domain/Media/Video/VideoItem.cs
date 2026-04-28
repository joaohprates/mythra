namespace Mythra.Domain.Media.Video;

public sealed class VideoItem : MediaItem
{
    public override MediaKind Kind => MediaKind.Video;

    public VideoKind VideoKind { get; set; }
    public bool IsAnime { get; set; }
    public TimeSpan? Duration { get; set; }

    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? Container { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public double? FrameRate { get; set; }
    public long? Bitrate { get; set; }

    public Guid? ParentId { get; set; }
    public int? SeasonNumber { get; set; }
    public int? EpisodeNumber { get; set; }
    public int? AbsoluteEpisodeNumber { get; set; }

    public List<Subtitle> Subtitles { get; set; } = [];
    public List<AudioTrack> AudioTracks { get; set; } = [];
    public List<ChapterMarker> ChapterMarkers { get; set; } = [];

    public string ResolutionLabel => (Width, Height) switch
    {
        (>= 3840, _) or (_, >= 2160) => "4K",
        (>= 1920, _) or (_, >= 1080) => "1080p",
        (>= 1280, _) or (_, >= 720) => "720p",
        (>= 854, _) or (_, >= 480) => "480p",
        _ => "SD",
    };
}
