using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Streaming;
using Mythra.Application.Services.Streaming;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/stream")]
public sealed class StreamController(
    IStreamingService streaming,
    IStreamSessionRepository sessions,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("start")]
    [Authorize]
    public async Task<IActionResult> Start([FromQuery] Guid profileId, [FromBody] StartStreamRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var ua = Request.Headers.UserAgent.ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await streaming.StartAsync(userId, profileId, req, ua, ip, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{token}")]
    [Authorize]
    public async Task<IActionResult> Stop(string token, CancellationToken ct) =>
        (await streaming.StopAsync(token, ct)).ToActionResult();

    [HttpGet("{token}/playlist.m3u8")]
    [AllowAnonymous]
    public async Task<IActionResult> Playlist(string token, CancellationToken ct)
    {
        var session = await sessions.GetByTokenAsync(token, ct);
        if (session?.PlaylistPath is null || !System.IO.File.Exists(session.PlaylistPath))
            return NotFound();
        var content = await System.IO.File.ReadAllTextAsync(session.PlaylistPath, ct);
        return Content(content, "application/vnd.apple.mpegurl");
    }

    [HttpGet("{token}/{segment}")]
    [AllowAnonymous]
    public async Task<IActionResult> Segment(string token, string segment, CancellationToken ct)
    {
        if (!segment.StartsWith("seg_") || !segment.EndsWith(".ts")) return BadRequest();
        var session = await sessions.GetByTokenAsync(token, ct);
        if (session?.PlaylistPath is null) return NotFound();
        var dir = Path.GetDirectoryName(session.PlaylistPath)!;
        var segmentPath = Path.Combine(dir, segment);
        if (!System.IO.File.Exists(segmentPath)) return NotFound();
        return PhysicalFile(segmentPath, "video/mp2t", enableRangeProcessing: true);
    }

    [HttpGet("probe/{videoItemId:guid}")]
    [Authorize]
    public async Task<IActionResult> Probe(Guid videoItemId, CancellationToken ct) =>
        (await streaming.ProbeAsync(videoItemId, ct)).ToActionResult();
}
