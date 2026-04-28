namespace Mythra.Domain.Media.Books;

public enum BookFormat
{
    Epub = 1,
    Pdf = 2,
    Mobi = 3,
    Azw3 = 4,
    Cbz = 5,
}

public sealed class BookItem : MediaItem
{
    public override MediaKind Kind => MediaKind.Book;

    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? Isbn { get; set; }
    public string? Series { get; set; }
    public int? SeriesIndex { get; set; }
    public BookFormat Format { get; set; } = BookFormat.Epub;
    public string FilePath { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public int? PageCount { get; set; }
    public int? WordCount { get; set; }

    public List<BookChapter> Chapters { get; set; } = [];
}
