using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Application.Abstractions.Persistence;
using SharpCompress.Archives;
using VersOne.Epub;

namespace Mythra.Api.Controllers;

[ApiController]
[Route("api/v1/items/{mediaItemId:guid}")]
[Authorize]
public sealed class ContentController(
    IMangaRepository mangas,
    IBookRepository books,
    IAudioRepository audios,
    ILogger<ContentController> log) : ControllerBase
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif", ".avif"];

    [HttpGet("chapters/{chapterId:guid}/pages/{pageIndex:int}")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> GetMangaPage(Guid mediaItemId, Guid chapterId, int pageIndex, CancellationToken ct)
    {
        var manga = await mangas.GetByIdWithChaptersAsync(mediaItemId, ct);
        if (manga is null) return NotFound();
        var chapter = manga.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null || !System.IO.File.Exists(chapter.ArchivePath)) return NotFound();
        if (pageIndex < 0 || pageIndex >= chapter.PageCount) return NotFound();

        try
        {
            using var archive = ArchiveFactory.Open(chapter.ArchivePath);
            var entries = archive.Entries
                .Where(e => !e.IsDirectory && ImageExtensions.Contains(Path.GetExtension(e.Key ?? "").ToLowerInvariant()))
                .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (pageIndex >= entries.Count) return NotFound();
            var entry = entries[pageIndex];

            using var stream = entry.OpenEntryStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            memory.Position = 0;

            var contentType = ContentTypeFor(entry.Key ?? "");
            return File(memory.ToArray(), contentType);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to extract manga page {Index} from {Path}", pageIndex, chapter.ArchivePath);
            return Problem(detail: "Failed to extract page.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("chapters/{chapterId:guid}/content")]
    public async Task<IActionResult> GetBookChapter(Guid mediaItemId, Guid chapterId, CancellationToken ct)
    {
        var book = await books.GetByIdWithChaptersAsync(mediaItemId, ct);
        if (book is null) return NotFound();
        var chapter = book.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null) return NotFound();

        if (book.Format != Mythra.Domain.Media.Books.BookFormat.Epub || string.IsNullOrEmpty(chapter.Anchor))
            return Ok(new
            {
                chapterId,
                title = chapter.Title,
                html = $"<p style='opacity:0.7'>This format ({book.Format}) does not yet support inline rendering. Open the source file directly.</p>",
            });

        try
        {
            var epub = await EpubReader.ReadBookAsync(book.FilePath);
            var contentFile = epub.ReadingOrder.FirstOrDefault(f => f.FilePath == chapter.Anchor);
            if (contentFile is null)
                return NotFound();
            return Ok(new { chapterId, title = chapter.Title, html = contentFile.Content });
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to extract book chapter content {Anchor} from {Path}", chapter.Anchor, book.FilePath);
            return Problem(detail: "Failed to extract chapter.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("chapters/{chapterId:guid}/stream")]
    [AllowAnonymous]
    public async Task<IActionResult> StreamAudioChapter(Guid mediaItemId, Guid chapterId, CancellationToken ct)
    {
        var audio = await audios.GetByIdWithChaptersAsync(mediaItemId, ct);
        if (audio is null) return NotFound();
        var chapter = audio.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null || !System.IO.File.Exists(chapter.FilePath)) return NotFound();

        var contentType = chapter.FilePath.ToLowerInvariant() switch
        {
            var s when s.EndsWith(".mp3") => "audio/mpeg",
            var s when s.EndsWith(".m4a") || s.EndsWith(".m4b") || s.EndsWith(".aac") => "audio/aac",
            var s when s.EndsWith(".flac") => "audio/flac",
            var s when s.EndsWith(".ogg") || s.EndsWith(".opus") => "audio/ogg",
            var s when s.EndsWith(".wav") => "audio/wav",
            _ => "application/octet-stream",
        };

        await Task.CompletedTask;
        return PhysicalFile(chapter.FilePath, contentType, enableRangeProcessing: true);
    }

    private static string ContentTypeFor(string filename) => Path.GetExtension(filename).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".gif" => "image/gif",
        ".avif" => "image/avif",
        _ => "image/jpeg",
    };
}
