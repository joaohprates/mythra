using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Services.Recommendations;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/recommendations")]
[Authorize]
public sealed class RecommendationsController(
    IRecommendationService recommendations) : ControllerBase
{
    /// <summary>
    /// Returns personalised recommendations for the active profile.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRecommendations(
        [FromQuery] Guid profileId,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        if (profileId == Guid.Empty)
            return BadRequest(new { error = "ProfileRequired", message = "profileId is required." });

        var result = await recommendations.GetForProfileAsync(profileId, take, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Returns the health status of all configured metadata providers.
    /// </summary>
    [HttpGet("providers/health")]
    public async Task<IActionResult> GetProviderHealth(CancellationToken ct)
    {
        var result = await recommendations.GetProviderHealthAsync(ct);
        return result.ToActionResult();
    }
}
