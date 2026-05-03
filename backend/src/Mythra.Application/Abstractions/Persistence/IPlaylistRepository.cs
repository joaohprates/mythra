using Mythra.Domain.Playlists;

namespace Mythra.Application.Abstractions.Persistence;

public interface IPlaylistRepository
{
    Task<Playlist?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Playlist?> GetWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Playlist>> ListByProfileAsync(Guid profileId, CancellationToken ct = default);
    Task AddAsync(Playlist playlist, CancellationToken ct = default);
    void Remove(Playlist playlist);
}
