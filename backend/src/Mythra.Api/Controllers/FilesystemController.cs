using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Application.Abstractions.Files;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/filesystem")]
[Authorize(Roles = "Admin,Manager")]
public sealed class FilesystemController(IFileSystem fs) : ControllerBase
{
    /// <summary>Lists immediate subdirectories of a given path.</summary>
    [HttpGet("browse")]
    public IActionResult Browse([FromQuery] string path = "/")
    {
        var normalized = path.Replace('\\', '/').TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized)) normalized = "/";

        if (!fs.DirectoryExists(normalized))
            return NotFound(new { detail = $"Directory not found: {normalized}" });

        var parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');

        var entries = fs.ListDirectories(normalized)
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .Select(e => new { e.Name, path = e.FullPath, e.IsReadable })
            .ToList();

        return Ok(new { current = normalized, parent, entries });
    }

    /// <summary>Validates whether a path exists and is accessible.</summary>
    [HttpGet("validate")]
    public IActionResult Validate([FromQuery] string path)
    {
        var normalized = path.Replace('\\', '/');
        return Ok(new
        {
            exists    = fs.DirectoryExists(normalized),
            isReadable = fs.IsReadable(normalized),
        });
    }
}
