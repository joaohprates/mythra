using Mythra.Domain.Libraries;

namespace Mythra.Application.Dtos.Libraries;

public sealed record LibraryDto(
    Guid Id,
    string Name,
    LibraryKind Kind,
    string? Description,
    bool IsEnabled,
    bool IsSystem,
    bool AutoRefreshMetadata,
    DateTimeOffset? LastScannedAt,
    int FolderCount,
    int? ItemCount);

public sealed record LibraryDetailDto(
    Guid Id,
    string Name,
    LibraryKind Kind,
    string? Description,
    bool IsEnabled,
    bool IsSystem,
    bool AutoRefreshMetadata,
    DateTimeOffset? LastScannedAt,
    string? PreferredLanguage,
    string? PreferredMetadataProvider,
    IReadOnlyList<string> EffectiveExtensions,
    IReadOnlyList<LibraryFolderDto> Folders);

public sealed record LibraryFolderDto(Guid Id, string Path, bool IsActive, DateTimeOffset? LastScannedAt);

public sealed record CreateLibraryRequest(
    string Name,
    LibraryKind Kind,
    string? Description,
    IReadOnlyList<string> Folders,
    string? PreferredLanguage = null,
    string? PreferredMetadataProvider = null);

public sealed record UpdateLibraryRequest(
    string? Name,
    string? Description,
    bool? IsEnabled,
    bool? AutoRefreshMetadata,
    string? PreferredLanguage,
    string? PreferredMetadataProvider);

public sealed record AddFolderRequest(string Path);

public sealed record UpdateFolderRequest(string? Path, bool? IsActive);
