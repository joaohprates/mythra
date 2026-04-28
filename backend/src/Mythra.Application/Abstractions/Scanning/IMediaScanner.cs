using Mythra.Domain.Libraries;

namespace Mythra.Application.Abstractions.Scanning;

public sealed record ScanResult(
    int Added,
    int Updated,
    int Removed,
    int Failed,
    TimeSpan Elapsed,
    IReadOnlyList<string> Errors);

public interface IMediaScanner
{
    LibraryKind Kind { get; }
    Task<ScanResult> ScanAsync(Guid libraryId, IReadOnlyList<string> rootPaths, CancellationToken ct = default);
}

public interface IMediaScannerRegistry
{
    IMediaScanner? Resolve(LibraryKind kind);
}
