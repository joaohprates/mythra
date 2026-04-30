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
}
