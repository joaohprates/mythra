using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Dtos.Search;
using Mythra.Application.Services.Search;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public sealed class SearchController(ISearchService search) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Search([FromBody] UnifiedSearchRequest req, CancellationToken ct) =>
        (await search.SearchAsync(req, ct)).ToActionResult();

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 30,
        CancellationToken ct = default)
    {
        var req = new UnifiedSearchRequest(q, null, null, null, null, skip, take);
        return (await search.SearchAsync(req, ct)).ToActionResult();
    }
}
