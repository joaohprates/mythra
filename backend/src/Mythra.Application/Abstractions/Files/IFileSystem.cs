namespace Mythra.Application.Abstractions.Files;

public sealed record FileEntry(string Path, string Name, long Size, DateTimeOffset LastModified, bool IsDirectory);

public sealed record DirectoryEntry(string Name, string FullPath, bool IsReadable);

public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    IEnumerable<FileEntry> EnumerateFiles(string path, string searchPattern = "*", bool recursive = true);
    IEnumerable<DirectoryEntry> ListDirectories(string path);
    bool IsReadable(string path);
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);
    Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default);
    void EnsureDirectoryExists(string path);
    void DeleteFile(string path);
    Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default);
}
