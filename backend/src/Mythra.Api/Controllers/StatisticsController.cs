using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Services.Statistics;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:guid}/statistics")]
[Authorize]
public sealed class StatisticsController(IStatisticsService statistics) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        Guid profileId,
        [FromQuery] int weeks = 12,
        CancellationToken ct = default) =>
        (await statistics.GetProfileStatisticsAsync(profileId, Math.Clamp(weeks, 1, 52), ct)).ToActionResult();
}
