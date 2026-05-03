using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Dtos.Playlists;
using Mythra.Application.Services.Playlists;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/profiles/{profileId:guid}/playlists")]
[Authorize]
public sealed class PlaylistsController(IPlaylistService playlists) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid profileId, CancellationToken ct) =>
        (await playlists.ListAsync(profileId, ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid profileId, Guid id, CancellationToken ct) =>
        (await playlists.GetAsync(id, profileId, ct)).ToActionResult();

    [HttpPost]
    public async Task<IActionResult> Create(Guid profileId, [FromBody] CreatePlaylistRequest req, CancellationToken ct)
    {
        var result = await playlists.CreateAsync(profileId, req, ct);
        return result.ToCreated($"api/v1/profiles/{profileId}/playlists/{result.Value?.Id}");
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid profileId, Guid id, [FromBody] UpdatePlaylistRequest req, CancellationToken ct) =>
        (await playlists.UpdateAsync(id, profileId, req, ct)).ToActionResult();

    [HttpPost("{id:guid}/items")]
    public async Task<IActionResult> AddItem(Guid profileId, Guid id, [FromBody] AddPlaylistItemRequest req, CancellationToken ct) =>
        (await playlists.AddItemAsync(id, profileId, req, ct)).ToActionResult();

    [HttpDelete("{id:guid}/items/{mediaItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid profileId, Guid id, Guid mediaItemId, CancellationToken ct) =>
        (await playlists.RemoveItemAsync(id, profileId, mediaItemId, ct)).ToActionResult();

    [HttpPatch("{id:guid}/items/reorder")]
    public async Task<IActionResult> ReorderItem(Guid profileId, Guid id, [FromBody] ReorderPlaylistItemRequest req, CancellationToken ct) =>
        (await playlists.ReorderItemAsync(id, profileId, req, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid profileId, Guid id, CancellationToken ct) =>
        (await playlists.DeleteAsync(id, profileId, ct)).ToActionResult();
}
