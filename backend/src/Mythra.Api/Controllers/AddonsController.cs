using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Api.Common;
using Mythra.Application.Abstractions.Auth;
using Mythra.Application.Services.Addons;
using System.Text;
using System.Text.Json;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/addons")]
[Authorize]
public sealed class AddonsController(
    IAddonService addonService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await addonService.ListAsync(currentUser.UserId.Value, ct)).ToActionResult();
    }

    /// <summary>
    /// Import an addon from a .mythra-addon.json file.
    /// The addon is created in PendingSecrets status. Call PATCH /{id}/configure
    /// to supply API keys and activate it.
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

        using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
        var json = await reader.ReadToEndAsync(ct);
        return (await addonService.ImportAsync(currentUser.UserId.Value, json, ct)).ToActionResult();
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var result = await addonService.ExportAsync(currentUser.UserId.Value, id, ct);
        if (result.IsFailure)
            return NotFound(new { result.Error.Code, result.Error.Message });

        var json = JsonSerializer.Serialize(result.Value,
            new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"addon-{id}.mythra-addon.json");
    }

    /// <summary>
    /// Supply secrets (API keys, tokens) and/or config values for an addon.
    /// Secrets are never returned in list/export responses.
    /// If all required secrets are now present, the addon is automatically activated.
    /// </summary>
    [HttpPatch("{id:guid}/configure")]
    public async Task<IActionResult> Configure(Guid id, [FromBody] ConfigureAddonRequest req, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await addonService.ConfigureAsync(currentUser.UserId.Value, id, req, ct)).ToActionResult();
    }

    /// <summary>Toggle between Active and Disabled. Cannot toggle PendingSecrets addons.</summary>
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await addonService.ToggleAsync(currentUser.UserId.Value, id, ct)).ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        return (await addonService.DeleteAsync(currentUser.UserId.Value, id, ct)).ToActionResult();
    }
}
