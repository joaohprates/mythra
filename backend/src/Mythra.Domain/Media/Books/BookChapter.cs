using Mythra.Domain.Common;

namespace Mythra.Domain.Media.Books;

public sealed class BookChapter : Entity
{
    public Guid BookItemId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Anchor { get; set; }
    public int? StartPage { get; set; }
    public int? EndPage { get; set; }
    public int? WordCount { get; set; }
}
