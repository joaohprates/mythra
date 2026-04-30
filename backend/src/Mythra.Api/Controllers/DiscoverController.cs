using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Services.Discover;
using Mythra.Domain.Media;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/discover")]
[Authorize]
public sealed class DiscoverController(IDiscoverService discover) : ControllerBase
{
    /// <summary>Search external metadata providers for content to import.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] string kind = "Video",
        [FromQuery] int skip = 0,
        [FromQuery] int take = 18,
        CancellationToken ct = default)
    {
        if (!Enum.TryParse<MediaKind>(kind, ignoreCase: true, out var mediaKind))
            return BadRequest(new { error = "InvalidKind", message = $"'{kind}' is not a valid media kind." });

        return (await discover.SearchAsync(q, mediaKind, skip, take, ct)).ToActionResult();
    }

    /// <summary>Import an external item into the local library (no file download — streams externally).</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportExternalRequest req, CancellationToken ct = default)
        => (await discover.ImportAsync(req, ct)).ToCreated("/api/v1/items/{0}");
}
