using Mythra.Domain.Common;

namespace Mythra.Domain.Media;

public sealed class Genre : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public MediaKind? Kind { get; set; }

    private Genre() { }

    public Genre(string name, MediaKind? kind = null)
    {
        Name = name.Trim();
        Slug = name.Trim().ToLowerInvariant().Replace(' ', '-');
        Kind = kind;
    }
}
