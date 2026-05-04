using System.Diagnostics;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Search;
using Mythra.Domain.Common;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Services.Search;

public sealed class SearchService(IMediaItemRepository media) : ISearchService
{
    public async Task<Result<UnifiedSearchResult>> SearchAsync(UnifiedSearchRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var hits = new List<SearchHit>();
        var kinds = request.Kinds is { Count: > 0 }
            ? request.Kinds
            : [MediaKind.Video, MediaKind.Manga, MediaKind.Book];

        var genreFilter = request.Genres is { Count: > 0 } ? request.Genres[0] : null;

        foreach (var kind in kinds.Distinct())
        {
            var items = await media.SearchAsync(new MediaQuery(
                Kind: kind,
                Search: string.IsNullOrWhiteSpace(request.Query) ? null : request.Query,
                Genre: genreFilter,
                Skip: 0,
                Take: request.Take), ct);

            foreach (var item in items)
            {
                if (request.YearFrom.HasValue && (item.Year ?? 0) < request.YearFrom) continue;
                if (request.YearTo.HasValue && (item.Year ?? int.MaxValue) > request.YearTo) continue;
                hits.Add(new SearchHit(
                    item.Id,
                    item.Kind,
                    item.Title,
                    BuildSubtitle(item),
                    item.PosterPath,
                    item.Year,
                    item.Rating,
                    Score(item.Title, request.Query)));
            }
        }

        var total = hits.Count;
        var paged = hits
            .OrderByDescending(h => h.Relevance)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToList();

        sw.Stop();
        return new UnifiedSearchResult(paged, total, sw.Elapsed.TotalMilliseconds);
    }

    private static string? BuildSubtitle(MediaItem item) => item switch
    {
        VideoItem v when v.VideoKind == VideoKind.Episode => $"S{v.SeasonNumber:D2}E{v.EpisodeNumber:D2}",
        VideoItem v => v.VideoKind.ToString(),
        MangaItem m => m.Author,
        BookItem b => b.Author,
        _ => null,
    };

    private static double Score(string title, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return 1.0;
        var t = title.ToLowerInvariant();
        var q = query.Trim().ToLowerInvariant();
        if (t == q) return 1.0;
        if (t.StartsWith(q)) return 0.9;
        if (t.Contains(' ' + q) || t.Contains(q + ' ')) return 0.8;
        if (t.Contains(q)) return 0.6;
        return 0.3;
    }
}
