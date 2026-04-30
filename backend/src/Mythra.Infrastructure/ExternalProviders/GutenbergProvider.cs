using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Retrieves free e-book links from Project Gutenberg via the GutenDex REST API.
/// Returns EPUB links where available, falls back to plain-text.
/// </summary>
public sealed class GutenbergProvider(
    HttpClient                         http,
    IOptions<ExternalProvidersOptions> options,
    ILogger<GutenbergProvider>         logger) : IExternalBookProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "Gutenberg";
    public int    Priority => 10;

    public bool Supports(MediaKind kind) =>
        _opts.GutendexEnabled && kind is MediaKind.Book;

    public async Task<IReadOnlyList<ExternalBookResult>> GetLinksAsync(
        ExternalBookRequest request,
        CancellationToken   ct = default)
    {
        if (!_opts.GutendexEnabled || request.Kind is not MediaKind.Book)
            return [];

        try
        {
            var query    = Uri.EscapeDataString(request.Title);
            var response = await http.GetFromJsonAsync<GutendexResponse>(
                $"{_opts.GutendexBaseUrl}/books?search={query}&mime_type=application%2Fepub",
                ct);

            if (response?.Results is null) return [];

            return response.Results
                .Select(ToResult)
                .OfType<ExternalBookResult>()
                .Take(5)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Gutenberg] Search failed for '{Title}'", request.Title);
            return [];
        }
    }

    private ExternalBookResult? ToResult(GutendexBook book)
    {
        if (book.Formats is null) return null;

        // Prefer EPUB, then plain text
        var url =
            book.Formats.GetValueOrDefault("application/epub+zip") ??
            book.Formats.GetValueOrDefault("text/plain; charset=utf-8") ??
            book.Formats.GetValueOrDefault("text/plain");

        if (string.IsNullOrWhiteSpace(url)) return null;

        // Use the MIME type key (not the URL) to determine format, because
        // Gutenberg URLs may end in ".epub.images" rather than ".epub"
        var isEpub = book.Formats.ContainsKey("application/epub+zip") &&
                     url == book.Formats["application/epub+zip"];
        var format = isEpub ? ExternalBookFormat.Epub : ExternalBookFormat.PlainText;

        return new ExternalBookResult(
            ProviderName: Name,
            Format:       format,
            Url:          url,
            CoverUrl:     book.Formats.GetValueOrDefault("image/jpeg"),
            Language:     book.Languages?.FirstOrDefault(),
            Authors:      book.Authors?.Select(a => a.Name).ToList());
    }

    // ── GutenDex JSON models ─────────────────────────────────────────────────

    private sealed record GutendexResponse(
        [property: JsonPropertyName("results")] IReadOnlyList<GutendexBook>? Results);

    private sealed record GutendexBook(
        [property: JsonPropertyName("title")]     string? Title,
        [property: JsonPropertyName("authors")]   IReadOnlyList<GutendexAuthor>? Authors,
        [property: JsonPropertyName("formats")]   IReadOnlyDictionary<string, string>? Formats,
        [property: JsonPropertyName("languages")] IReadOnlyList<string>? Languages);

    private sealed record GutendexAuthor(
        [property: JsonPropertyName("name")] string Name);
}
