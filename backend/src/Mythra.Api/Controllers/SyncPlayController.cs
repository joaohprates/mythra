using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Dtos.SyncPlay;
using Mythra.Application.Services.SyncPlay;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/syncplay")]
[Authorize]
public sealed class SyncPlayController(ISyncPlayService sync, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost("rooms")]
    public async Task<IActionResult> Create([FromBody] CreateSyncRoomRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var username = currentUser.Username ?? "Unknown";
        return (await sync.CreateRoomAsync(userId, username, req, ct)).ToActionResult();
    }

    [HttpPost("rooms/join")]
    public async Task<IActionResult> Join([FromBody] JoinSyncRoomRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return (await sync.JoinAsync(userId, req, ct)).ToActionResult();
    }

    [HttpDelete("rooms/{code}")]
    public async Task<IActionResult> Leave(string code, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return (await sync.LeaveAsync(userId, code, ct)).ToActionResult();
    }

    [HttpGet("rooms/{code}")]
    public async Task<IActionResult> Get(string code, CancellationToken ct) =>
        (await sync.GetAsync(code, ct)).ToActionResult();

    [HttpPost("rooms/{code}/command")]
    public async Task<IActionResult> Command(string code, [FromBody] SyncCommandDto cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        return (await sync.ApplyCommandAsync(userId, code, cmd, ct)).ToActionResult();
    }
}
