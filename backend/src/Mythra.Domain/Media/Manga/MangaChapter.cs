using Mythra.Domain.Common;

namespace Mythra.Domain.Media.Manga;

public sealed class MangaChapter : Entity
{
    public Guid MangaItemId { get; set; }
    public int? VolumeNumber { get; set; }
    public double ChapterNumber { get; set; }
    public string? Title { get; set; }
    public string ArchivePath { get; set; } = string.Empty;
    public string ArchiveFormat { get; set; } = "cbz";
    public int PageCount { get; set; }
    public string? CoverPath { get; set; }
    public DateOnly? ReleaseDate { get; set; }
}
