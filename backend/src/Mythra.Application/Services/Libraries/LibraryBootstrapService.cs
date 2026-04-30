using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Libraries;

namespace Mythra.Application.Services.Libraries;

public sealed class LibraryBootstrapService(
    ILibraryRepository libraries,
    IUnitOfWork uow,
    ILogger<LibraryBootstrapService> log) : ILibraryBootstrapService
{
    private const string DefaultMediaPath = "/media";
    private const string GeneralLibraryName = "General";

    public async Task EnsureDefaultLibraryAsync(CancellationToken ct = default)
    {
        var existing = await libraries.GetByNameAsync(GeneralLibraryName, ct);
        if (existing is not null)
        {
            log.LogDebug("System library '{Name}' already exists — skipping bootstrap.", GeneralLibraryName);
            return;
        }

        var lib = new Library(GeneralLibraryName, LibraryKind.General)
        {
            IsSystem = true,
            Description = "Default library — scans all media types from /media.",
            IsEnabled = true,
            AutoRefreshMetadata = true,
        };
        lib.AddFolder(DefaultMediaPath);

        await libraries.AddAsync(lib, ct);
        await uow.SaveChangesAsync(ct);
        log.LogInformation("Bootstrapped system library '{Name}' pointing to {Path}.", GeneralLibraryName, DefaultMediaPath);
    }
}
