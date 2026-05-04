using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Video;
using System.Text.Json;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/download")]
[Authorize]
public sealed class DownloadController(
    IMediaItemRepository mediaRepo) : ControllerBase
{
    /// <summary>
    /// Streams the original file for a media item as a download.
    /// Only works for items with FileStatus = "Available".
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> DownloadFile(Guid id, CancellationToken ct)
    {
        var item = await mediaRepo.GetByIdWithDetailsAsync(id, ct);
        if (item is null) return NotFound();

        if (item.FileStatus != "Available")
            return BadRequest(new { error = "FileUnavailable", message = $"File status is '{item.FileStatus}'. Only available files can be downloaded." });

        string? filePath = item switch
        {
            VideoItem v => v.FilePath,
            BookItem  b => b.FilePath,
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            return NotFound(new { error = "FileNotFound", message = "The file does not exist on disk." });

        var fileName = Path.GetFileName(filePath);
        var contentType = GetMimeType(filePath);

        return PhysicalFile(filePath, contentType, fileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// Exports metadata for a media item as a JSON or NFO file.
    /// </summary>
    [HttpGet("{id:guid}/metadata")]
    public async Task<IActionResult> ExportMetadata(Guid id, [FromQuery] string format = "json", CancellationToken ct = default)
    {
        var item = await mediaRepo.GetByIdWithDetailsAsync(id, ct);
        if (item is null) return NotFound();

        if (format.Equals("nfo", StringComparison.OrdinalIgnoreCase))
        {
            var nfo = BuildNfo(item);
            return Content(nfo, "application/xml");
        }

        // Default: JSON
        var dto = new
        {
            item.Id,
            item.Title,
            item.OriginalTitle,
            item.Overview,
            item.Tagline,
            Kind = item.Kind.ToString(),
            item.ReleaseDate,
            item.Rating,
            item.Language,
            Genres = item.Genres.Select(g => g.Name).ToList(),
            Tags = item.Tags.Select(t => t.Name).ToList(),
            item.PosterPath,
            item.BackdropPath,
            item.FileStatus,
            ProviderIds = new
            {
                Tmdb   = item.ProviderTmdbId,
                Imdb   = item.ProviderImdbId,
                Anilist = item.ProviderAnilistId,
            },
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        });

        var fileName = SanitizeFileName(item.Title) + ".json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string GetMimeType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".mp4"  => "video/mp4",
        ".mkv"  => "video/x-matroska",
        ".webm" => "video/webm",
        ".avi"  => "video/x-msvideo",
        ".epub" => "application/epub+zip",
        ".pdf"  => "application/pdf",
        ".cbz"  => "application/zip",
        ".mp3"  => "audio/mpeg",
        ".m4a"  or ".m4b" => "audio/mp4",
        ".flac" => "audio/flac",
        ".ogg"  => "audio/ogg",
        _       => "application/octet-stream",
    };

    private static string BuildNfo(MediaItem item) =>
        $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <movie>
          <title>{System.Security.SecurityElement.Escape(item.Title)}</title>
          <originaltitle>{System.Security.SecurityElement.Escape(item.OriginalTitle ?? item.Title)}</originaltitle>
          <plot>{System.Security.SecurityElement.Escape(item.Overview ?? string.Empty)}</plot>
          <tagline>{System.Security.SecurityElement.Escape(item.Tagline ?? string.Empty)}</tagline>
          <rating>{item.Rating?.ToString("F1") ?? "0.0"}</rating>
          <year>{item.ReleaseDate?.Year}</year>
          <language>{item.Language ?? "en"}</language>
        {string.Join("\n  ", item.Genres.Select(g => $"  <genre>{System.Security.SecurityElement.Escape(g.Name)}</genre>"))}
        {(item.ProviderTmdbId is not null ? $"  <tmdbid>{item.ProviderTmdbId}</tmdbid>" : "")}
        {(item.ProviderImdbId is not null ? $"  <imdbid>{item.ProviderImdbId}</imdbid>" : "")}
        </movie>
        """;

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}
