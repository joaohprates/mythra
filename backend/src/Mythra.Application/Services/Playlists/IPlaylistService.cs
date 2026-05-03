using Mythra.Application.Dtos.Playlists;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Playlists;

public interface IPlaylistService
{
    Task<Result<IReadOnlyList<PlaylistDto>>> ListAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<PlaylistDetailDto>> GetAsync(Guid id, Guid profileId, CancellationToken ct = default);
    Task<Result<PlaylistDetailDto>> CreateAsync(Guid profileId, CreatePlaylistRequest req, CancellationToken ct = default);
    Task<Result<PlaylistDetailDto>> UpdateAsync(Guid id, Guid profileId, UpdatePlaylistRequest req, CancellationToken ct = default);
    Task<Result<PlaylistDetailDto>> AddItemAsync(Guid id, Guid profileId, AddPlaylistItemRequest req, CancellationToken ct = default);
    Task<Result<PlaylistDetailDto>> RemoveItemAsync(Guid id, Guid profileId, Guid mediaItemId, CancellationToken ct = default);
    Task<Result<PlaylistDetailDto>> ReorderItemAsync(Guid id, Guid profileId, ReorderPlaylistItemRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, Guid profileId, CancellationToken ct = default);
}
