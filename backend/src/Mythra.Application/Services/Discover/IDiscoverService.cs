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
    string? ExistingItemId,
    bool IsAdult = false);

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

/// <summary>
/// Discover query — supports two modes:
///   • Catalog mode (no Query): browse curated lists by Type/Category.
///   • Search mode (Query set): free-text search across providers.
/// </summary>
public sealed record DiscoverQuery(
    string? Query,
    MediaKind Kind,
    string Type,         // "movie" | "series" | "anime" | "manga" | "book" | "music"
    string Category,     // "popular" | "trending" | "top" | "year" | "rating"
    int Skip,
    int Take,
    string? Provider);

public interface IDiscoverService
{
    Task<Result<DiscoverResultDto>> SearchAsync(DiscoverQuery query, CancellationToken ct = default);

    Task<Result<ImportResultDto>> ImportAsync(
        ImportExternalRequest req,
        CancellationToken ct = default);
}
