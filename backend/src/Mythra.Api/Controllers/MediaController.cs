using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Services.Media;
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
        CancellationToken ct = default)
    {
        var query = new MediaQuery(libraryId, kind, search, genre, year, skip, take, orderBy);
        var result = await media.ListAsync(query, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => (await media.GetDetailAsync(id, ct)).ToActionResult();

    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> Summary(Guid id, CancellationToken ct) => (await media.GetSummaryAsync(id, ct)).ToActionResult();

    [HttpGet("recently-added")]
    public async Task<IActionResult> RecentlyAdded([FromQuery] Guid? libraryId, [FromQuery] int take = 20, CancellationToken ct = default) =>
        (await media.RecentlyAddedAsync(libraryId, take, ct)).ToActionResult();

    [HttpGet("genres")]
    public async Task<IActionResult> Genres([FromQuery] MediaKind? kind, CancellationToken ct) =>
        (await media.ListGenresAsync(kind, ct)).ToActionResult();
}
