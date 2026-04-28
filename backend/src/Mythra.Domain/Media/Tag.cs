using Mythra.Domain.Common;

namespace Mythra.Domain.Media;

public sealed class Tag : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    private Tag() { }

    public Tag(string name)
    {
        Name = name.Trim();
        Slug = name.Trim().ToLowerInvariant().Replace(' ', '-');
    }
}
