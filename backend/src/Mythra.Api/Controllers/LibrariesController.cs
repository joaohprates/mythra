using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Dtos.Libraries;
using Mythra.Application.Services.Libraries;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/libraries")]
[Authorize]
public sealed class LibrariesController(ILibraryService libraries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => (await libraries.ListAsync(ct)).ToActionResult();

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => (await libraries.GetAsync(id, ct)).ToActionResult();

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateLibraryRequest req, CancellationToken ct) =>
        (await libraries.CreateAsync(req, ct)).ToCreated("/api/v1/libraries/{0}");

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLibraryRequest req, CancellationToken ct) =>
        (await libraries.UpdateAsync(id, req, ct)).ToActionResult();

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        (await libraries.DeleteAsync(id, ct)).ToActionResult();

    [HttpPost("{id:guid}/folders")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AddFolder(Guid id, [FromBody] AddFolderRequest req, CancellationToken ct) =>
        (await libraries.AddFolderAsync(id, req, ct)).ToActionResult();

    [HttpDelete("{id:guid}/folders/{folderId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveFolder(Guid id, Guid folderId, CancellationToken ct) =>
        (await libraries.RemoveFolderAsync(id, folderId, ct)).ToActionResult();

    [HttpPost("{id:guid}/scan")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Scan(Guid id, CancellationToken ct) =>
        (await libraries.EnqueueScanAsync(id, ct)).ToActionResult();
}
