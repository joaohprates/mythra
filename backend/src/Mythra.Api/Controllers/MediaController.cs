using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Services.Media;
using Mythra.Domain.Common;
using Mythra.Domain.Media;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/items")]
[Authorize]
public sealed class MediaController(IMediaService media) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? libraryId,
        [FromQuery] MediaKind? kind,
        [FromQuery] string? search,
        [FromQuery] string? genre,
        [FromQuery] int? year,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string orderBy = "title",
        [FromQuery] string? ids = null,
        [FromQuery] bool? isAdult = null,
        CancellationToken ct = default)
    {
        IReadOnlyList<Guid>? parsedIds = null;
        if (!string.IsNullOrWhiteSpace(ids))
        {
            parsedIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : (Guid?)null)
                           .Where(g => g.HasValue)
                           .Select(g => g!.Value)
                           .ToList();
        }
        var query = new MediaQuery(libraryId, kind, search, genre, year, skip, take, orderBy, parsedIds, isAdult);
        var result = await media.ListAsync(query, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var guid = await ResolveIdAsync(id, ct);
        if (!guid.IsSuccess) return guid.ToActionResult();
        return (await media.GetDetailAsync(guid.Value, ct)).ToActionResult();
    }

    [HttpGet("{id}/summary")]
    public async Task<IActionResult> Summary(string id, CancellationToken ct)
    {
        var guid = await ResolveIdAsync(id, ct);
        if (!guid.IsSuccess) return guid.ToActionResult();
        return (await media.GetSummaryAsync(guid.Value, ct)).ToActionResult();
    }

    [HttpGet("recently-added")]
    public async Task<IActionResult> RecentlyAdded([FromQuery] Guid? libraryId, [FromQuery] int take = 20, CancellationToken ct = default) =>
        (await media.RecentlyAddedAsync(libraryId, take, ct)).ToActionResult();

    [HttpGet("{id}/episodes")]
    public async Task<IActionResult> Episodes(string id, CancellationToken ct)
    {
        var guid = await ResolveIdAsync(id, ct);
        if (!guid.IsSuccess) return guid.ToActionResult();
        return (await media.ListEpisodesAsync(guid.Value, ct)).ToActionResult();
    }

    [HttpGet("genres")]
    public async Task<IActionResult> Genres([FromQuery] MediaKind? kind, CancellationToken ct) =>
        (await media.ListGenresAsync(kind, ct)).ToActionResult();

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var guid = await ResolveIdAsync(id, ct);
        if (!guid.IsSuccess) return guid.ToActionResult();
        return (await media.DeleteAsync(guid.Value, ct)).ToActionResult();
    }

    private async Task<Result<Guid>> ResolveIdAsync(string id, CancellationToken ct)
    {
        if (Guid.TryParse(id, out var guid)) return guid;
        return await media.FindIdByExternalAsync(id, ct);
    }
}
