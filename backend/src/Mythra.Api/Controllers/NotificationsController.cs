using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Services.Notifications;
using System.Text.Json;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(
    INotificationService notifications,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await notifications.ListAsync(currentUser.UserId.Value, unreadOnly, skip, take, ct)).ToActionResult();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct = default)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var count = await notifications.GetUnreadCountAsync(currentUser.UserId.Value, ct);
        return count.IsSuccess ? Ok(new { count = count.Value }) : count.ToActionResult();
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await notifications.MarkReadAsync(currentUser.UserId.Value, id, ct)).ToActionResult();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await notifications.MarkAllReadAsync(currentUser.UserId.Value, ct)).ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await notifications.DeleteAsync(currentUser.UserId.Value, id, ct)).ToActionResult();
    }

    /// <summary>Server-Sent Events stream for real-time notifications.</summary>
    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct = default)
    {
        if (currentUser.UserId is null) { Response.StatusCode = 401; return; }

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");
        await Response.Body.FlushAsync(ct);

        await foreach (var dto in notifications.StreamAsync(currentUser.UserId.Value, ct))
        {
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var line = $"data: {json}\n\n";
            await Response.WriteAsync(line, ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
