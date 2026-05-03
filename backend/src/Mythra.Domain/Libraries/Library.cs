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
    public bool IsSystem { get; set; } = false;
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

    /// <summary>Returns the default extensions for this library kind.</summary>
    public IReadOnlyList<string> GetEffectiveExtensions() => GetDefaultExtensions(Kind);

    public static IReadOnlyList<string> GetDefaultExtensions(LibraryKind kind) => kind switch
    {
        LibraryKind.Video or LibraryKind.Anime =>
            [".mp4", ".mkv", ".m4v", ".mov", ".avi", ".webm", ".ts", ".mpg", ".mpeg", ".wmv", ".m2ts"],
        LibraryKind.Book =>
            [".epub", ".pdf", ".mobi", ".azw3", ".fb2", ".txt"],
        LibraryKind.Manga =>
            [".cbz", ".cbr", ".cb7"],
        LibraryKind.Audiobook or LibraryKind.Music =>
            [".mp3", ".flac", ".m4a", ".ogg", ".wav", ".opus", ".aac", ".m4b"],
        LibraryKind.Image =>
            [".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".bmp", ".tiff"],
        LibraryKind.General =>
            [".mp4", ".mkv", ".m4v", ".mov", ".avi", ".webm", ".ts",
             ".epub", ".pdf", ".mobi", ".azw3", ".fb2",
             ".cbz", ".cbr", ".cb7",
             ".mp3", ".flac", ".m4a", ".ogg", ".wav", ".opus", ".aac", ".m4b",
             ".jpg", ".jpeg", ".png", ".gif", ".webp"],
        _ => [],
    };
}
