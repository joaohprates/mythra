using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Scanning;

public interface IScanService
{
    Task<Result<ScanResult>> RunAsync(Guid libraryId, CancellationToken ct = default);
}
