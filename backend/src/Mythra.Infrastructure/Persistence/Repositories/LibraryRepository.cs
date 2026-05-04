using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Libraries;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class LibraryRepository(MythraDbContext db) : EfRepository<Library>(db), ILibraryRepository
{
    public async Task<IReadOnlyList<Library>> ListAsync(CancellationToken ct = default) =>
        await Set.Include(l => l.Folders).OrderBy(l => l.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Library>> ListByKindAsync(LibraryKind kind, CancellationToken ct = default) =>
        await Set.Include(l => l.Folders).Where(l => l.Kind == kind).OrderBy(l => l.Name).ToListAsync(ct);

    public Task<Library?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(l => l.Name == name, ct);

    public Task<Library?> GetWithFoldersAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(l => l.Folders).FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<Library?> GetSystemLibraryAsync(CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(l => l.IsSystem && l.IsEnabled, ct);

    public async Task DeleteWithCascadeAsync(Guid libraryId, CancellationToken ct = default)
    {
        await using var trx = await Db.Database.BeginTransactionAsync(ct);

        var mediaIds = await Db.MediaItems
            .Where(m => m.LibraryId == libraryId)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (mediaIds.Count > 0)
        {
            await Db.Subtitles.Where(s => mediaIds.Contains(s.VideoItemId)).ExecuteDeleteAsync(ct);
            await Db.AudioTracks.Where(a => mediaIds.Contains(a.VideoItemId)).ExecuteDeleteAsync(ct);
            await Db.ChapterMarkers.Where(c => mediaIds.Contains(c.VideoItemId)).ExecuteDeleteAsync(ct);
            await Db.MangaChapters.Where(c => mediaIds.Contains(c.MangaItemId)).ExecuteDeleteAsync(ct);
            await Db.BookChapters.Where(c => mediaIds.Contains(c.BookItemId)).ExecuteDeleteAsync(ct);
            await Db.Playbacks.Where(p => mediaIds.Contains(p.MediaItemId)).ExecuteDeleteAsync(ct);
            await Db.Readings.Where(r => mediaIds.Contains(r.MediaItemId)).ExecuteDeleteAsync(ct);
            await Db.Bookmarks.Where(b => mediaIds.Contains(b.MediaItemId)).ExecuteDeleteAsync(ct);
            await Db.Highlights.Where(h => mediaIds.Contains(h.MediaItemId)).ExecuteDeleteAsync(ct);
            await Db.Favorites.Where(f => mediaIds.Contains(f.MediaItemId)).ExecuteDeleteAsync(ct);
            await Db.PlaylistItems.Where(p => mediaIds.Contains(p.MediaItemId)).ExecuteDeleteAsync(ct);
            await Db.MediaPersonRoles.Where(r => mediaIds.Contains(r.MediaItemId)).ExecuteDeleteAsync(ct);
            // Clear M:N join tables (genres/tags) before deleting parents.
            await Db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM media_item_genres WHERE MediaItemId IN (SELECT Id FROM media_items WHERE LibraryId = '{libraryId}')", ct);
            await Db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM media_item_tags WHERE MediaItemId IN (SELECT Id FROM media_items WHERE LibraryId = '{libraryId}')", ct);
            await Db.MediaItems.Where(m => m.LibraryId == libraryId).ExecuteDeleteAsync(ct);
        }

        await Db.LibraryFolders.Where(f => f.LibraryId == libraryId).ExecuteDeleteAsync(ct);
        await Db.Libraries.Where(l => l.Id == libraryId).ExecuteDeleteAsync(ct);

        await trx.CommitAsync(ct);
    }
}
