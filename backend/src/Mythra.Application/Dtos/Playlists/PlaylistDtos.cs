using Mythra.Domain.Media;

namespace Mythra.Application.Dtos.Playlists;

public sealed record PlaylistDto(
    Guid Id,
    Guid ProfileId,
    string Name,
    string? Description,
    bool IsPublic,
    string? CoverImagePath,
    int ItemCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PlaylistDetailDto(
    Guid Id,
    Guid ProfileId,
    string Name,
    string? Description,
    bool IsPublic,
    string? CoverImagePath,
    IReadOnlyList<PlaylistItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record PlaylistItemDto(
    Guid Id,
    Guid MediaItemId,
    string Title,
    MediaKind Kind,
    string? PosterPath,
    double? Rating,
    int? Year,
    int Order,
    DateTimeOffset AddedAt);

public sealed record CreatePlaylistRequest(
    string Name,
    string? Description = null,
    bool IsPublic = false);

public sealed record UpdatePlaylistRequest(
    string? Name,
    string? Description,
    bool? IsPublic);

public sealed record AddPlaylistItemRequest(Guid MediaItemId);

public sealed record ReorderPlaylistItemRequest(Guid MediaItemId, int NewOrder);
