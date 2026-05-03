using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Favorites;

namespace Mythra.Api.Controllers;

public sealed record AddFavoriteRequest(Guid MediaItemId);
public sealed record FavoriteItemDto(Guid Id, Guid ProfileId, Guid MediaItemId, DateTime AddedAt);
public sealed record FavoriteStatusDto(bool IsFavorite);

[ApiController]
[Route("api/v1/profiles/{profileId:guid}/favorites")]
[Authorize]
public sealed class FavoritesController(
    IFavoriteRepository favorites,
    IProfileRepository profiles,
    IUnitOfWork uow,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid profileId, CancellationToken ct)
    {
        if (!await VerifyOwnership(profileId, ct)) return Forbid();
        var items = await favorites.GetByProfileAsync(profileId, ct);
        return Ok(items.Select(ToDto));
    }

    [HttpGet("{mediaItemId:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid profileId, Guid mediaItemId, CancellationToken ct)
    {
        if (!await VerifyOwnership(profileId, ct)) return Forbid();
        var exists = await favorites.ExistsAsync(profileId, mediaItemId, ct);
        return Ok(new FavoriteStatusDto(exists));
    }

    [HttpPost]
    public async Task<IActionResult> Add(Guid profileId, [FromBody] AddFavoriteRequest req, CancellationToken ct)
    {
        if (!await VerifyOwnership(profileId, ct)) return Forbid();

        if (await favorites.ExistsAsync(profileId, req.MediaItemId, ct))
            return Conflict(new { error = "AlreadyFavorited", message = "Item is already in favorites." });

        var item = FavoriteItem.Create(profileId, req.MediaItemId);
        favorites.Add(item);
        await uow.SaveChangesAsync(ct);

        return Created($"api/v1/profiles/{profileId}/favorites/{req.MediaItemId}/status", ToDto(item));
    }

    [HttpDelete("{mediaItemId:guid}")]
    public async Task<IActionResult> Remove(Guid profileId, Guid mediaItemId, CancellationToken ct)
    {
        if (!await VerifyOwnership(profileId, ct)) return Forbid();

        var item = await favorites.GetAsync(profileId, mediaItemId, ct);
        if (item is null) return NotFound(new { error = "NotFound", message = "Favorite not found." });

        favorites.Remove(item);
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<bool> VerifyOwnership(Guid profileId, CancellationToken ct)
    {
        var profile = await profiles.GetByIdAsync(profileId, ct);
        return profile is not null && profile.UserId == currentUser.UserId;
    }

    private static FavoriteItemDto ToDto(FavoriteItem f) =>
        new(f.Id, f.ProfileId, f.MediaItemId, f.AddedAt);
}
