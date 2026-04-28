using Mythra.Domain.Libraries;

namespace Mythra.Application.Abstractions.Persistence;

public interface ILibraryRepository : IRepository<Library>
{
    Task<IReadOnlyList<Library>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Library>> ListByKindAsync(LibraryKind kind, CancellationToken ct = default);
    Task<Library?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Library?> GetWithFoldersAsync(Guid id, CancellationToken ct = default);
}
