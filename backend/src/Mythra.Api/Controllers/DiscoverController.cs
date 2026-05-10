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
    /// <summary>
    /// Discover content. Two modes:
    ///   • Catalog browsing: leave <c>q</c> empty, pass <c>type</c> + <c>category</c>.
    ///   • Search: pass a <c>q</c> string.
    /// Pagination via <c>page</c> (1-based) or raw <c>skip</c>/<c>take</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] string type = "movie",                  // movie | series | anime | manga | book | music
        [FromQuery] string category = "popular",            // popular | trending | top | year | rating
        [FromQuery] string? kind = null,                    // optional MediaKind override
        [FromQuery] string? provider = null,
        [FromQuery] string? genre = null,
        [FromQuery] int page = 1,
        [FromQuery] int? skip = null,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var resolvedKind = ResolveKind(kind, type);
        if (resolvedKind is null)
            return BadRequest(new { error = "InvalidKind", message = $"Unsupported kind/type combination: kind='{kind}' type='{type}'." });

        var safeTake = Math.Clamp(take, 1, 60);
        var safeSkip = skip ?? Math.Max(0, (page - 1) * safeTake);

        var query = new DiscoverQuery(
            Query:    string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Kind:     resolvedKind.Value,
            Type:     NormalizeType(type),
            Category: string.IsNullOrWhiteSpace(category) ? "popular" : category.Trim().ToLowerInvariant(),
            Skip:     safeSkip,
            Take:     safeTake,
            Provider: provider,
            Genre:    string.IsNullOrWhiteSpace(genre) ? null : genre.Trim());

        return (await discover.SearchAsync(query, ct)).ToActionResult();
    }

    /// <summary>Import an external item into the local library (no file download — streams externally).</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] ImportExternalRequest req, CancellationToken ct = default)
        => (await discover.ImportAsync(req, ct)).ToCreated("/api/v1/items/{0}");

    private static MediaKind? ResolveKind(string? explicitKind, string type)
    {
        if (!string.IsNullOrWhiteSpace(explicitKind)
            && Enum.TryParse<MediaKind>(explicitKind, ignoreCase: true, out var parsed))
            return parsed;

        return type?.ToLowerInvariant() switch
        {
            "movie" or "series" or "anime" => MediaKind.Video,
            "manga" => MediaKind.Manga,
            "book"  => MediaKind.Book,
            _ => null,
        };
    }

    private static string NormalizeType(string type) => type?.ToLowerInvariant() switch
    {
        "movie" or "movies"  => "movie",
        "series" or "tv"     => "series",
        "anime"              => "anime",
        "manga"              => "manga",
        "book" or "books"    => "book",
        _ => "movie",
    };
}
