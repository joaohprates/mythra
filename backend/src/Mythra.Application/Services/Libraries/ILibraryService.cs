using Mythra.Application.Dtos.Libraries;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Libraries;

public interface ILibraryService
{
    Task<Result<IReadOnlyList<LibraryDto>>> ListAsync(CancellationToken ct = default);
    Task<Result<LibraryDetailDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<LibraryDetailDto>> CreateAsync(CreateLibraryRequest req, CancellationToken ct = default);
    Task<Result<LibraryDetailDto>> UpdateAsync(Guid id, UpdateLibraryRequest req, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<LibraryDetailDto>> AddFolderAsync(Guid id, AddFolderRequest req, CancellationToken ct = default);
    Task<Result> RemoveFolderAsync(Guid id, Guid folderId, CancellationToken ct = default);
    Task<Result<Guid>> EnqueueScanAsync(Guid id, CancellationToken ct = default);
}
