using Mythra.Application.Dtos.Statistics;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Statistics;

public interface IStatisticsService
{
    Task<Result<ProfileStatisticsDto>> GetProfileStatisticsAsync(Guid profileId, int weekCount = 12, CancellationToken ct = default);
}
