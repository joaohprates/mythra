using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Libraries;

namespace Mythra.Infrastructure.Scanning;

public sealed class MediaScannerRegistry(IEnumerable<IMediaScanner> scanners) : IMediaScannerRegistry
{
    private readonly Dictionary<LibraryKind, IMediaScanner> _byKind = scanners.ToDictionary(s => s.Kind);

    public IMediaScanner? Resolve(LibraryKind kind) =>
        _byKind.TryGetValue(kind, out var scanner) ? scanner :
        kind == LibraryKind.Anime    && _byKind.TryGetValue(LibraryKind.Video,     out var video)  ? video  :
        kind == LibraryKind.General  && _byKind.TryGetValue(LibraryKind.General,    out var general)? general:
        null;
}
