using Mythra.Domain.Common;

namespace Mythra.Domain.Libraries;

public sealed class LibraryFolder : Entity
{
    public Guid LibraryId { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastScannedAt { get; set; }

    private LibraryFolder() { }

    public LibraryFolder(string path)
    {
        Path = path;
    }
}
