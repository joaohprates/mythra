using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Application.Services.Discover;

public sealed record DiscoverItemDto(
    string ExternalId,
    string ProviderKind,
    string Title,
    string? OriginalTitle,
    int? Year,
    double? Rating,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    IReadOnlyList<string> Genres,
    bool AlreadyImported,
    string? ExistingItemId);

public sealed record DiscoverResultDto(
    IReadOnlyList<DiscoverItemDto> Items,
    int Total,
    int Skip,
    int Take);

public sealed record ImportExternalRequest(
    string ProviderKind,
    string ExternalId,
    MediaKind MediaKind,
    Guid? TargetLibraryId = null);

public sealed record ImportResultDto(
    Guid Id,
    string Title,
    string Kind,
    bool HasFile,
    string FileStatus,
    string? PosterPath,
    Guid LibraryId,
    string WatchUrl);

public interface IDiscoverService
{
    Task<Result<DiscoverResultDto>> SearchAsync(
        string query,
        MediaKind kind,
        int skip,
        int take,
        CancellationToken ct = default);

    Task<Result<ImportResultDto>> ImportAsync(
        ImportExternalRequest req,
        CancellationToken ct = default);
}
