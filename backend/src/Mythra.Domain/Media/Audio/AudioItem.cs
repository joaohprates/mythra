namespace Mythra.Domain.Media.Audio;

public enum AudioKind
{
    Audiobook = 1,
    Podcast = 2,
    Music = 3,
    Soundtrack = 4,
}

public sealed class AudioItem : MediaItem
{
    public override MediaKind Kind => MediaKind.Audio;

    public AudioKind AudioKind { get; set; } = AudioKind.Audiobook;
    public string? Author { get; set; }
    public string? Narrator { get; set; }
    public string? Series { get; set; }
    public int? SeriesIndex { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? RootPath { get; set; }
    public string? CoverPath { get; set; }

    public List<AudioChapter> Chapters { get; set; } = [];
}
