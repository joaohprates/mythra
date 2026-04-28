using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mythra.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health() => Ok(new
    {
        status = "ok",
        service = "mythra",
        version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "0.1.0",
        timestamp = DateTimeOffset.UtcNow,
    });

    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { pong = DateTimeOffset.UtcNow });
}
