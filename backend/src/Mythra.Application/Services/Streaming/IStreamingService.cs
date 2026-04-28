using Mythra.Application.Dtos.Streaming;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Streaming;

public interface IStreamingService
{
    Task<Result<StreamSessionDto>> StartAsync(Guid userId, Guid profileId, StartStreamRequest req, string? userAgent, string? ip, CancellationToken ct = default);
    Task<Result> StopAsync(string sessionToken, CancellationToken ct = default);
    Task<Result<StreamProbeDto?>> ProbeAsync(Guid videoItemId, CancellationToken ct = default);
}
