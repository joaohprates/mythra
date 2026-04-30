using Mythra.Domain.Common;

namespace Mythra.Application.Services.Media;

public interface IMetadataEnrichmentService
{
    Task<Result> EnrichAsync(Guid mediaItemId, string? preferredProvider = null, CancellationToken ct = default);
}
