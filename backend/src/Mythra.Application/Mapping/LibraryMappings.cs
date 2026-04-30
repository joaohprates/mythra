using Mythra.Application.Dtos.Libraries;
using Mythra.Domain.Libraries;

namespace Mythra.Application.Mapping;

public static class LibraryMappings
{
    public static LibraryDto ToDto(this Library l, int? itemCount = null) => new(
        l.Id,
        l.Name,
        l.Kind,
        l.Description,
        l.IsEnabled,
        l.IsSystem,
        l.AutoRefreshMetadata,
        l.LastScannedAt,
        l.Folders.Count,
        itemCount,
        l.AllowedExtensions.AsReadOnly());

    public static LibraryDetailDto ToDetail(this Library l) => new(
        l.Id,
        l.Name,
        l.Kind,
        l.Description,
        l.IsEnabled,
        l.IsSystem,
        l.AutoRefreshMetadata,
        l.LastScannedAt,
        l.PreferredLanguage,
        l.PreferredMetadataProvider,
        l.AllowedExtensions.AsReadOnly(),
        l.GetEffectiveExtensions(),
        l.Folders.Select(f => new LibraryFolderDto(f.Id, f.Path, f.IsActive, f.LastScannedAt)).ToList());
}
