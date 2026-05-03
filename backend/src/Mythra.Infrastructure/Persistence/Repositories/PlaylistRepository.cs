using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Playlists;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class PlaylistRepository(MythraDbContext db) : IPlaylistRepository
{
    public Task<Playlist?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Playlists.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Playlist?> GetWithItemsAsync(Guid id, CancellationToken ct = default)
        => db.Playlists
             .Include(p => p.Items)
             .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Playlist>> ListByProfileAsync(Guid profileId, CancellationToken ct = default)
        => await db.Playlists
                   .Include(p => p.Items)
                   .Where(p => p.ProfileId == profileId)
                   .OrderBy(p => p.Name)
                   .ToListAsync(ct);

    public async Task AddAsync(Playlist playlist, CancellationToken ct = default)
        => await db.Playlists.AddAsync(playlist, ct);

    public void Remove(Playlist playlist)
        => db.Playlists.Remove(playlist);
}
