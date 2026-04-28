using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;

namespace Mythra.Domain.Libraries;

public sealed class Library : AggregateRoot
{
    public string Name { get; set; } = string.Empty;
    public LibraryKind Kind { get; set; }
    public string? Description { get; set; }
    public string? PreferredLanguage { get; set; }
    public string? PreferredMetadataProvider { get; set; }
    public bool AutoRefreshMetadata { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset? LastScannedAt { get; set; }

    public List<LibraryFolder> Folders { get; set; } = [];

    private Library() { }

    public Library(string name, LibraryKind kind)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvariantViolationException("Library name cannot be empty.");
        Name = name.Trim();
        Kind = kind;
    }

    public void AddFolder(string path)
    {
        if (Folders.Any(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException($"Folder '{path}' is already part of this library.");
        Folders.Add(new LibraryFolder(path));
        Touch();
    }

    public void MarkScanned()
    {
        LastScannedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new InvariantViolationException("Library name cannot be empty.");
        Name = newName.Trim();
        Touch();
    }
}
