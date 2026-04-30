using Mythra.Application.Abstractions.Files;

namespace Mythra.Infrastructure.Files;

public sealed class LocalFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public IEnumerable<FileEntry> EnumerateFiles(string path, string searchPattern = "*", bool recursive = true)
    {
        if (!Directory.Exists(path)) yield break;
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(path, searchPattern, option))
        {
            FileInfo info;
            try { info = new FileInfo(file); }
            catch { continue; }
            yield return new FileEntry(info.FullName, info.Name, info.Length, info.LastWriteTimeUtc, IsDirectory: false);
        }
    }

    public IEnumerable<DirectoryEntry> ListDirectories(string path)
    {
        if (!Directory.Exists(path)) yield break;
        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            string name;
            bool readable;
            try
            {
                var info = new DirectoryInfo(dir);
                name = info.Name;
                // Quick readability check — try to enumerate top level
                Directory.GetDirectories(dir);
                readable = true;
            }
            catch
            {
                name = Path.GetFileName(dir);
                readable = false;
            }
            yield return new DirectoryEntry(name, dir.Replace('\\', '/'), readable);
        }
    }

    public bool IsReadable(string path)
    {
        if (!Directory.Exists(path)) return false;
        try { Directory.GetDirectories(path); return true; }
        catch { return false; }
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default) =>
        Task.FromResult<Stream>(File.OpenRead(path));

    public async Task WriteAllBytesAsync(string path, byte[] data, CancellationToken ct = default)
    {
        EnsureDirectoryExists(Path.GetDirectoryName(path) ?? ".");
        await File.WriteAllBytesAsync(path, data, ct);
    }

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
    }

    public void DeleteFile(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken ct = default) =>
        File.ReadAllBytesAsync(path, ct);
}
