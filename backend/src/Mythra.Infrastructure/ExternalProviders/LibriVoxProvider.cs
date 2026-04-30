using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.ExternalProviders;

/// <summary>
/// Retrieves free audiobook links from the LibriVox public API.
/// Returns a ZIP-file URL containing MP3 chapters (or the LibriVox catalog page as fallback).
/// </summary>
public sealed class LibriVoxProvider(
    HttpClient                         http,
    IOptions<ExternalProvidersOptions> options,
    ILogger<LibriVoxProvider>          logger) : IExternalBookProvider
{
    private readonly ExternalProvidersOptions _opts = options.Value;

    public string Name     => "LibriVox";
    public int    Priority => 20;

    public bool Supports(MediaKind kind) =>
        _opts.LibriVoxEnabled && kind is MediaKind.Audio;

    public async Task<IReadOnlyList<ExternalBookResult>> GetLinksAsync(
        ExternalBookRequest request,
        CancellationToken   ct = default)
    {
        if (!_opts.LibriVoxEnabled || request.Kind is not MediaKind.Audio)
            return [];

        try
        {
            var query    = Uri.EscapeDataString(request.Title);
            var response = await http.GetFromJsonAsync<LibriVoxResponse>(
                $"{_opts.LibriVoxBaseUrl}/audiobooks?title={query}&format=json&limit=5",
                ct);

            if (response?.Books is null) return [];

            return response.Books
                .Select(book => new ExternalBookResult(
                    ProviderName: Name,
                    Format:       ExternalBookFormat.Mp3,
                    Url:          book.UrlZipFile ?? book.UrlLibrivox ?? string.Empty,
                    Language:     book.Language,
                    Authors:      book.Authors?
                        .Select(a => $"{a.FirstName} {a.LastName}".Trim())
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList()))
                .Where(r => !string.IsNullOrEmpty(r.Url))
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[LibriVox] Search failed for '{Title}'", request.Title);
            return [];
        }
    }

    // ── LibriVox JSON models ─────────────────────────────────────────────────

    private sealed record LibriVoxResponse(
        [property: JsonPropertyName("books")] IReadOnlyList<LibriVoxBook>? Books);

    private sealed record LibriVoxBook(
        [property: JsonPropertyName("title")]        string? Title,
        [property: JsonPropertyName("language")]     string? Language,
        [property: JsonPropertyName("url_zip_file")] string? UrlZipFile,
        [property: JsonPropertyName("url_librivox")] string? UrlLibrivox,
        [property: JsonPropertyName("authors")]      IReadOnlyList<LibriVoxAuthor>? Authors);

    private sealed record LibriVoxAuthor(
        [property: JsonPropertyName("first_name")] string? FirstName,
        [property: JsonPropertyName("last_name")]  string? LastName);
}
