using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Media.Books;
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

        // Non-EPUB: tell the client to download instead
        if (book.Format != BookFormat.Epub)
            return Ok(new
            {
                chapterId,
                title = "Unsupported format",
                html = string.Empty,
                unsupported = true,
                format = book.Format.ToString(),
                downloadUrl = $"/api/v1/download/{mediaItemId}",
            });

        if (string.IsNullOrEmpty(book.FilePath) || !System.IO.File.Exists(book.FilePath))
            return NotFound();

        var chapter = book.Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter is null) return NotFound();

        try
        {
            var epub = await EpubReader.ReadBookAsync(book.FilePath);

            // Build image data-URI map so relative <img src> tags resolve
            var imageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var img in epub.Content.Images.Local)
            {
                try
                {
                    var ext = Path.GetExtension(img.FilePath).TrimStart('.').ToLowerInvariant();
                    var mime = ext switch
                    {
                        "png" => "image/png",
                        "gif" => "image/gif",
                        "webp" => "image/webp",
                        "svg" => "image/svg+xml",
                        _ => "image/jpeg",
                    };
                    var data = Convert.ToBase64String(img.Content);
                    var dataUri = $"data:{mime};base64,{data}";
                    imageMap[img.FilePath] = dataUri;
                    imageMap[Path.GetFileName(img.FilePath)] = dataUri;
                }
                catch { /* skip unreadable images */ }
            }

            // Locate the reading-order file. Strip fragment (#) from anchor first.
            var anchor = chapter.Anchor?.Split('#')[0];
            var readingOrder = epub.ReadingOrder;
            var contentFile =
                // exact match
                readingOrder.FirstOrDefault(f => string.Equals(f.FilePath, anchor, StringComparison.OrdinalIgnoreCase))
                // suffix match (handles path-prefix differences)
                ?? readingOrder.FirstOrDefault(f => anchor != null &&
                    (f.FilePath?.EndsWith(anchor, StringComparison.OrdinalIgnoreCase) == true
                     || anchor.EndsWith(Path.GetFileName(f.FilePath ?? ""), StringComparison.OrdinalIgnoreCase)))
                // fall back to chapter order index
                ?? (chapter.Order < readingOrder.Count ? readingOrder[chapter.Order] : null);

            if (contentFile is null)
                return NotFound();

            var html = ProcessEpubHtml(contentFile.Content ?? string.Empty, imageMap);
            return Ok(new { chapterId, title = chapter.Title, html });
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to extract EPUB chapter {Anchor} from {Path}", chapter.Anchor, book.FilePath);
            return Problem(detail: "Failed to extract chapter.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Returns a JSON list of reading-order file paths for an EPUB — useful when the book
    /// has no navigation entries and the client needs to construct its own chapter list.
    /// </summary>
    [HttpGet("reading-order")]
    public async Task<IActionResult> GetReadingOrder(Guid mediaItemId, CancellationToken ct)
    {
        var book = await books.GetByIdWithChaptersAsync(mediaItemId, ct);
        if (book is null || book.Format != BookFormat.Epub) return NotFound();
        if (string.IsNullOrEmpty(book.FilePath) || !System.IO.File.Exists(book.FilePath)) return NotFound();

        try
        {
            var epub = await EpubReader.ReadBookAsync(book.FilePath);
            var items = epub.ReadingOrder
                .Select((f, i) => new { id = i, title = $"Part {i + 1}", filePath = f.FilePath })
                .ToList();
            return Ok(items);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to read EPUB reading order from {Path}", book.FilePath);
            return Problem(detail: "Failed to read epub.", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    // ── EPUB HTML processing ─────────────────────────────────────────────────

    private static string ProcessEpubHtml(string html, Dictionary<string, string> imageMap)
    {
        // Extract <body> content only (discard EPUB XHTML boilerplate)
        var bodyMatch = Regex.Match(html, @"<body[^>]*>(.*?)</body>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (bodyMatch.Success) html = bodyMatch.Groups[1].Value;

        // Strip <script> tags entirely
        html = Regex.Replace(html, @"<script[^>]*>.*?</script>", string.Empty,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Strip <link> (external CSS, fonts, etc.) – we inject our own
        html = Regex.Replace(html, @"<link\b[^>]*/?>", string.Empty, RegexOptions.IgnoreCase);

        // Strip inline <style> blocks that could break the reader's theme
        html = Regex.Replace(html, @"<style[^>]*>.*?</style>", string.Empty,
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Replace img src with data URIs
        html = Regex.Replace(html, @"src=[""']([^""']+)[""']", m =>
        {
            var src = m.Groups[1].Value;
            if (imageMap.TryGetValue(src, out var uri) ||
                imageMap.TryGetValue(Path.GetFileName(src), out uri))
                return $"src=\"{uri}\"";
            return m.Value;
        }, RegexOptions.IgnoreCase);

        // Remove href anchors that would navigate away
        html = Regex.Replace(html, @"<a\b([^>]*)href=[""'][^""']*[""']([^>]*)>",
            "<a$1$2>", RegexOptions.IgnoreCase);

        return html;
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
