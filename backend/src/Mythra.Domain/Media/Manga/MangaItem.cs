namespace Mythra.Domain.Media.Manga;

public enum MangaReadingDirection
{
    LeftToRight = 1,
    RightToLeft = 2,
    Vertical = 3,
}

public sealed class MangaItem : MediaItem
{
    public override MediaKind Kind => MediaKind.Manga;

    public string? Author { get; set; }
    public string? Artist { get; set; }
    public string? Status { get; set; }
    public MangaReadingDirection ReadingDirection { get; set; } = MangaReadingDirection.RightToLeft;
    public int? TotalChapters { get; set; }
    public int? TotalVolumes { get; set; }
    public string? RootPath { get; set; }

    public List<MangaChapter> Chapters { get; set; } = [];
}
