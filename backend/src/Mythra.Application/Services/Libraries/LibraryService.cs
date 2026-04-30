using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Background;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Libraries;
using Mythra.Application.Mapping;
using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;
using Mythra.Domain.Libraries;

namespace Mythra.Application.Services.Libraries;

public sealed class LibraryService(
    ILibraryRepository libraries,
    IBackgroundJobQueue jobs,
    IUnitOfWork uow,
    ILogger<LibraryService> log) : ILibraryService
{
    public async Task<Result<IReadOnlyList<LibraryDto>>> ListAsync(CancellationToken ct = default)
    {
        var list = await libraries.ListAsync(ct);
        return Result<IReadOnlyList<LibraryDto>>.Success(list.Select(l => l.ToDto()).ToList());
    }

    public async Task<Result<LibraryDetailDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(id, ct);
        return lib is null ? Error.NotFound("Library", id) : lib.ToDetail();
    }

    public async Task<Result<LibraryDetailDto>> CreateAsync(CreateLibraryRequest req, CancellationToken ct = default)
    {
        if (await libraries.GetByNameAsync(req.Name, ct) is not null)
            return Error.Conflict("Library name already exists.");
        try
        {
            var lib = new Library(req.Name, req.Kind)
            {
                Description = req.Description,
                PreferredLanguage = req.PreferredLanguage,
                PreferredMetadataProvider = req.PreferredMetadataProvider,
            };
            if (req.AllowedExtensions is { Count: > 0 })
                lib.AllowedExtensions = req.AllowedExtensions
                    .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}")
                    .Distinct()
                    .ToList();

            foreach (var f in req.Folders.Distinct(StringComparer.OrdinalIgnoreCase)) lib.AddFolder(f);
            await libraries.AddAsync(lib, ct);
            await uow.SaveChangesAsync(ct);
            log.LogInformation("Created library {Library} ({Kind})", lib.Name, lib.Kind);
            return lib.ToDetail();
        }
        catch (InvariantViolationException ex)
        {
            return Error.Validation(ex.Message);
        }
    }

    public async Task<Result<LibraryDetailDto>> UpdateAsync(Guid id, UpdateLibraryRequest req, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);

        if (req.Name is not null) lib.Rename(req.Name);
        if (req.Description is not null) lib.Description = req.Description;
        if (req.IsEnabled.HasValue) lib.IsEnabled = req.IsEnabled.Value;
        if (req.AutoRefreshMetadata.HasValue) lib.AutoRefreshMetadata = req.AutoRefreshMetadata.Value;
        if (req.PreferredLanguage is not null) lib.PreferredLanguage = req.PreferredLanguage;
        if (req.PreferredMetadataProvider is not null) lib.PreferredMetadataProvider = req.PreferredMetadataProvider;
        if (req.AllowedExtensions is not null)
            lib.AllowedExtensions = req.AllowedExtensions
                .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}")
                .Distinct()
                .ToList();

        lib.Touch();
        await uow.SaveChangesAsync(ct);
        return lib.ToDetail();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lib = await libraries.GetByIdAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);
        if (lib.IsSystem) return Error.Validation("System libraries cannot be deleted.");
        libraries.Remove(lib);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<LibraryDetailDto>> AddFolderAsync(Guid id, AddFolderRequest req, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);
        try
        {
            lib.AddFolder(req.Path);
            await uow.SaveChangesAsync(ct);
            return lib.ToDetail();
        }
        catch (InvariantViolationException ex)
        {
            return Error.Validation(ex.Message);
        }
    }

    public async Task<Result<LibraryDetailDto>> UpdateFolderAsync(Guid id, Guid folderId, UpdateFolderRequest req, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);

        var folder = lib.Folders.FirstOrDefault(f => f.Id == folderId);
        if (folder is null) return Error.NotFound("LibraryFolder", folderId);

        if (req.Path is not null)
        {
            var trimmed = req.Path.Trim();
            if (lib.Folders.Any(f => f.Id != folderId &&
                string.Equals(f.Path, trimmed, StringComparison.OrdinalIgnoreCase)))
                return Error.Conflict($"Folder '{trimmed}' is already part of this library.");
            folder.Path = trimmed;
        }

        if (req.IsActive.HasValue) folder.IsActive = req.IsActive.Value;
        lib.Touch();
        await uow.SaveChangesAsync(ct);
        log.LogInformation("Updated folder {FolderId} on library {LibraryId}", folderId, id);
        return lib.ToDetail();
    }

    public async Task<Result> RemoveFolderAsync(Guid id, Guid folderId, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);
        var folder = lib.Folders.FirstOrDefault(f => f.Id == folderId);
        if (folder is null) return Error.NotFound("LibraryFolder", folderId);
        lib.Folders.Remove(folder);
        lib.Touch();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<LibraryDetailDto>> UpdateExtensionsAsync(Guid id, UpdateExtensionsRequest req, CancellationToken ct = default)
    {
        var lib = await libraries.GetWithFoldersAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);

        lib.AllowedExtensions = req.Extensions
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrEmpty(e))
            .Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : $".{e.ToLowerInvariant()}")
            .Distinct()
            .ToList();

        lib.Touch();
        await uow.SaveChangesAsync(ct);
        return lib.ToDetail();
    }

    public async Task<Result<Guid>> EnqueueScanAsync(Guid id, CancellationToken ct = default)
    {
        var lib = await libraries.GetByIdAsync(id, ct);
        if (lib is null) return Error.NotFound("Library", id);
        var jobId = Guid.NewGuid();
        await jobs.EnqueueAsync(new ScanLibraryJob(jobId, id), ct);
        log.LogInformation("Enqueued scan job {JobId} for library {LibraryId}", jobId, id);
        return jobId;
    }
}
