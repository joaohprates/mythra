using Mythra.Domain.Common;

namespace Mythra.Domain.Progress;

public sealed class Bookmark : Entity
{
    public Guid ProfileId { get; set; }
    public Guid MediaItemId { get; set; }
    public string? Label { get; set; }
    public string? Note { get; set; }
    public TimeSpan? Position { get; set; }
    public int? Page { get; set; }
    public string? CfiLocator { get; set; }
}
