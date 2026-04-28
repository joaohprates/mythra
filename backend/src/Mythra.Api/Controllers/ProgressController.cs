using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Dtos.Progress;
using Mythra.Application.Services.Progress;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:guid}")]
[Authorize]
public sealed class ProgressController(IProgressService progress) : ControllerBase
{
    [HttpGet("playback/{mediaItemId:guid}")]
    public async Task<IActionResult> GetPlayback(Guid profileId, Guid mediaItemId, CancellationToken ct) =>
        (await progress.GetPlaybackAsync(profileId, mediaItemId, ct)).ToActionResult();

    [HttpPut("playback/{mediaItemId:guid}")]
    public async Task<IActionResult> UpdatePlayback(Guid profileId, Guid mediaItemId, [FromBody] UpdatePlaybackRequest req, CancellationToken ct) =>
        (await progress.UpdatePlaybackAsync(profileId, mediaItemId, req, ct)).ToActionResult();

    [HttpGet("continue-watching")]
    public async Task<IActionResult> ContinueWatching(Guid profileId, [FromQuery] int take = 20, CancellationToken ct = default) =>
        (await progress.ContinueWatchingAsync(profileId, take, ct)).ToActionResult();

    [HttpGet("reading/{mediaItemId:guid}")]
    public async Task<IActionResult> GetReading(Guid profileId, Guid mediaItemId, CancellationToken ct) =>
        (await progress.GetReadingAsync(profileId, mediaItemId, ct)).ToActionResult();

    [HttpPut("reading/{mediaItemId:guid}")]
    public async Task<IActionResult> UpdateReading(Guid profileId, Guid mediaItemId, [FromBody] UpdateReadingRequest req, CancellationToken ct) =>
        (await progress.UpdateReadingAsync(profileId, mediaItemId, req, ct)).ToActionResult();

    [HttpGet("continue-reading")]
    public async Task<IActionResult> ContinueReading(Guid profileId, [FromQuery] int take = 20, CancellationToken ct = default) =>
        (await progress.ContinueReadingAsync(profileId, take, ct)).ToActionResult();

    [HttpGet("bookmarks/{mediaItemId:guid}")]
    public async Task<IActionResult> ListBookmarks(Guid profileId, Guid mediaItemId, CancellationToken ct) =>
        (await progress.ListBookmarksAsync(profileId, mediaItemId, ct)).ToActionResult();

    [HttpPost("bookmarks/{mediaItemId:guid}")]
    public async Task<IActionResult> AddBookmark(Guid profileId, Guid mediaItemId, [FromBody] CreateBookmarkRequest req, CancellationToken ct) =>
        (await progress.AddBookmarkAsync(profileId, mediaItemId, req, ct)).ToActionResult();

    [HttpDelete("bookmarks/{bookmarkId:guid}")]
    public async Task<IActionResult> RemoveBookmark(Guid profileId, Guid bookmarkId, CancellationToken ct) =>
        (await progress.RemoveBookmarkAsync(profileId, bookmarkId, ct)).ToActionResult();

    [HttpGet("highlights/{mediaItemId:guid}")]
    public async Task<IActionResult> ListHighlights(Guid profileId, Guid mediaItemId, CancellationToken ct) =>
        (await progress.ListHighlightsAsync(profileId, mediaItemId, ct)).ToActionResult();

    [HttpPost("highlights/{mediaItemId:guid}")]
    public async Task<IActionResult> AddHighlight(Guid profileId, Guid mediaItemId, [FromBody] CreateHighlightRequest req, CancellationToken ct) =>
        (await progress.AddHighlightAsync(profileId, mediaItemId, req, ct)).ToActionResult();

    [HttpDelete("highlights/{highlightId:guid}")]
    public async Task<IActionResult> RemoveHighlight(Guid profileId, Guid highlightId, CancellationToken ct) =>
        (await progress.RemoveHighlightAsync(profileId, highlightId, ct)).ToActionResult();
}
