using Mythra.Domain.Media;

namespace Mythra.Application.Abstractions.Providers;

public enum ExternalBookFormat
{
    Epub      = 1,
    Pdf       = 2,
    PlainText = 3,
    Mp3       = 4,   // Audiobook chapter / archive zip
    WebReader = 5,   // Online reader URL (manga, web comics)
}

/// <summary>All identifiers and context Mythra can supply to a book/audio/manga provider.</summary>
public sealed record ExternalBookRequest(
    Guid      MediaItemId,
    string    Title,
    MediaKind Kind,
    string?   Author        = null,
    string?   GutenbergId   = null,
    string?   LibriVoxId    = null,
    string?   GoogleBooksId = null,
    string?   MangaDexId    = null,
    string?   Isbn          = null,
    string?   OpenLibraryId = null);

/// <summary>A single downloadable or readable link resolved by a provider.</summary>
public sealed record ExternalBookResult(
    string             ProviderName,
    ExternalBookFormat Format,
    string             Url,
    string?            CoverUrl = null,
    string?            Language = null,
    IReadOnlyList<string>? Authors = null);

public interface IExternalBookProvider
{
    string Name { get; }

    /// <summary>Lower value = tried first.</summary>
    int Priority { get; }

    bool Supports(MediaKind kind);

    Task<IReadOnlyList<ExternalBookResult>> GetLinksAsync(
        ExternalBookRequest request,
        CancellationToken   ct = default);
}
