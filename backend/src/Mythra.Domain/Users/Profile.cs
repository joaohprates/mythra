using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;
using Mythra.Domain.Media;

namespace Mythra.Domain.Users;

public sealed class Profile : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public bool IsKidFriendly { get; set; }
    public string Theme { get; set; } = "mythra-dark";

    public List<MediaKind> EnabledMediaKinds { get; set; } = [
        MediaKind.Video, MediaKind.Manga, MediaKind.Book, MediaKind.Audio];

    private Profile() { }

    public Profile(Guid userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvariantViolationException("Profile name is required.");
        UserId = userId;
        Name = name.Trim();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new InvariantViolationException("Profile name is required.");
        Name = newName.Trim();
        Touch();
    }
}
