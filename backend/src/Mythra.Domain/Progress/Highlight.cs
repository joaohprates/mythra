using Mythra.Domain.Common;

namespace Mythra.Domain.Progress;

public sealed class Highlight : Entity
{
    public Guid ProfileId { get; set; }
    public Guid MediaItemId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Color { get; set; } = "#A855F7";
    public string? CfiStart { get; set; }
    public string? CfiEnd { get; set; }
    public int? Page { get; set; }
}
