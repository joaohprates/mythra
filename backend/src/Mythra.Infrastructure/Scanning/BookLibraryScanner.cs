using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Files;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Scanning;
using Mythra.Domain.Libraries;
using Mythra.Domain.Media.Books;
using UglyToad.PdfPig;
using VersOne.Epub;

namespace Mythra.Infrastructure.Scanning;

public sealed class BookLibraryScanner(
    IFileSystem fs,
    IBookRepository books,
    IUnitOfWork uow,
    ILogger<BookLibraryScanner> log) : IMediaScanner
{
    private static readonly string[] BookExtensions = [".epub", ".pdf", ".mobi", ".azw3"];

    public LibraryKind Kind => LibraryKind.Book;

    public async Task<ScanResult> ScanAsync(Guid libraryId, IReadOnlyList<string> rootPaths, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var added = 0;
        var updated = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var root in rootPaths)
        {
            if (!fs.DirectoryExists(root))
            {
                errors.Add($"Folder not found: {root}");
                continue;
            }

            foreach (var entry in fs.EnumerateFiles(root, "*", recursive: true))
            {
                ct.ThrowIfCancellationRequested();
                var ext = Path.GetExtension(entry.Path).ToLowerInvariant();
                if (!BookExtensions.Contains(ext)) continue;

                try
                {
                    var existing = await books.GetByPathAsync(entry.Path, ct);
                    var book = existing ?? new BookItem
                    {
                        LibraryId = libraryId,
                        FilePath = entry.Path,
                        FileSizeBytes = entry.Size,
                        Format = ext switch
                        {
                            ".epub" => BookFormat.Epub,
                            ".pdf" => BookFormat.Pdf,
                            ".mobi" => BookFormat.Mobi,
                            ".azw3" => BookFormat.Azw3,
                            _ => BookFormat.Epub,
                        },
                        Title = Path.GetFileNameWithoutExtension(entry.Path),
                    };
                    book.FileSizeBytes = entry.Size;
                    book.LastScannedAt = DateTimeOffset.UtcNow;

                    switch (ext)
                    {
                        case ".epub": EnrichFromEpub(book, entry.Path); break;
                        case ".pdf": EnrichFromPdf(book, entry.Path); break;
                    }

                    if (existing is null) { await books.AddAsync(book, ct); added++; } else updated++;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"{entry.Path}: {ex.Message}");
                    log.LogWarning(ex, "Book scan failed for {Path}", entry.Path);
                }
            }
        }

        await uow.SaveChangesAsync(ct);
        sw.Stop();
        return new ScanResult(added, updated, Removed: 0, failed, sw.Elapsed, errors);
    }

    private static void EnrichFromEpub(BookItem book, string path)
    {
        try
        {
            var epub = EpubReader.ReadBook(path);
            book.Title = string.IsNullOrWhiteSpace(epub.Title) ? book.Title : epub.Title;
            book.Author = epub.AuthorList?.FirstOrDefault();
            book.Overview = epub.Description;
            book.Chapters = epub.Navigation?
                .Select((nav, i) => new BookChapter
                {
                    BookItemId = book.Id,
                    Order = i,
                    Title = nav.Title,
                    Anchor = nav.Link?.ContentFilePath,
                })
                .ToList() ?? [];
            book.PageCount = epub.ReadingOrder?.Count ?? 0;
        }
        catch { /* swallow — keep filesystem-derived defaults */ }
    }

    private static void EnrichFromPdf(BookItem book, string path)
    {
        try
        {
            using var pdf = PdfDocument.Open(path);
            book.PageCount = pdf.NumberOfPages;
            if (pdf.Information.Title is { Length: > 0 } t) book.Title = t;
            if (pdf.Information.Author is { Length: > 0 } a) book.Author = a;
        }
        catch { /* swallow — keep defaults */ }
    }
}
