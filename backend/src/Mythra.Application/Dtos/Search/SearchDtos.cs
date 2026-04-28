using Mythra.Domain.Media;

namespace Mythra.Application.Dtos.Search;

public sealed record UnifiedSearchRequest(
    string Query,
    IReadOnlyList<MediaKind>? Kinds,
    IReadOnlyList<string>? Genres,
    int? YearFrom,
    int? YearTo,
    int Skip = 0,
    int Take = 30);

public sealed record SearchHit(
    Guid Id,
    MediaKind Kind,
    string Title,
    string? Subtitle,
    string? PosterPath,
    int? Year,
    double? Rating,
    double Relevance);

public sealed record UnifiedSearchResult(IReadOnlyList<SearchHit> Hits, int Total, double ElapsedMs);
