namespace Mythra.Infrastructure.Streaming;

public sealed class FfmpegOptions
{
    public const string SectionName = "Ffmpeg";

    public string FfmpegPath { get; set; } = "ffmpeg";
    public string FfprobePath { get; set; } = "ffprobe";
    public int DefaultSegmentSeconds { get; set; } = 6;
    public int DefaultListSize { get; set; } = 0;
    public string DefaultPreset { get; set; } = "veryfast";
    public string DefaultCrf { get; set; } = "22";
    public string TranscodeRoot { get; set; } = "";
    public int ProbeTimeoutSeconds { get; set; } = 30;
}
