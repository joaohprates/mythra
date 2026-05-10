using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Mythra.Addon.MangaDex.Models;
using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;

namespace Mythra.Addon.MangaDex;

/// <summary>
/// Mythra addon that fetches manga chapters and pages from the official MangaDex API.
///
/// Flow:
///   1. GetLinksAsync is called with a title (and optionally a MangaDexId).
///   2. If no MangaDexId: search by title, score matches, cache the resolved ID.
///   3. Fetch chapters filtered by preferred languages.
///   4. Deduplicate (one chapter number per language set) and sort by volume → chapter.
///   5. GetPagesAsync fetches image URLs from the MangaDex at-home server.
///
/// Rate limits: MangaDex allows ~5 req/s for at-home and ~1500 req/10min general.
/// Caching keeps repeated reads within limits for a personal media hub.
///
/// Permissions required: Network, Cache, ReadConfig.
/// No API key required — MangaDex public API is free.
/// </summary>
public sealed class MangaDexAddon : IBookSourceAddon
{
    public string Id      => "io.mythra.mangadex";
    public string Name    => "MangaDex";
    public string Version => "1.0.0";

    private const string BaseUrl = "https://api.mangadex.org/";
    private static readonly string[] FallbackLanguages = ["pt-br", "pt", "en"];

    private IAddonContext _ctx = null!;
    private HttpClient    _http = null!;
    private string[]      _preferredLanguages = FallbackLanguages;
    private bool          _dataSaver;
    private int           _priority = 100;
    private TimeSpan      _searchCacheTtl;
    private TimeSpan      _chapterCacheTtl;
    private TimeSpan      _pagesCacheTtl;

    public int Priority => _priority;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public ValueTask InitializeAsync(IAddonContext context, CancellationToken ct = default)
    {
        _ctx  = context;
        _http = context.GetHttpClient(BaseUrl);

        var langs = context.GetConfig("PreferredLanguages");
        if (!string.IsNullOrWhiteSpace(langs))
            _preferredLanguages = langs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        _dataSaver = context.GetConfig("DataSaver") is "true" or "True" or "1";

        if (int.TryParse(context.GetConfig("Priority"), out var p)) _priority = p;

        _searchCacheTtl  = TryParseMinutes(context.GetConfig("SearchCacheTtlMinutes"),  60);
        _chapterCacheTtl = TryParseMinutes(context.GetConfig("ChapterCacheTtlMinutes"),  30);
        _pagesCacheTtl   = TryParseMinutes(context.GetConfig("PagesCacheTtlMinutes"),    10);

        _ctx.Logger.LogInformation(
            "[MangaDex] Initialized. Languages={Langs}, DataSaver={DS}, Priority={P}",
            string.Join(",", _preferredLanguages), _dataSaver, _priority);

        return ValueTask.CompletedTask;
    }

    public async ValueTask<AddonHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("ping", ct);
            return response.IsSuccessStatusCode
                ? AddonHealthStatus.Healthy
                : AddonHealthStatus.Degraded;
        }
        catch
        {
            return AddonHealthStatus.Unhealthy;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public bool Supports(AddonMediaKind kind) => kind == AddonMediaKind.Manga;

    // ── IBookSourceAddon ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns chapter links for the given manga. Each link encodes the chapter ID
    /// using the scheme <c>mangadex://chapter/{chapterId}</c> so the host can call
    /// GetPagesAsync later without re-fetching the chapter list.
    /// </summary>
    public async Task<IReadOnlyList<AddonBookResult>> GetLinksAsync(
        AddonBookRequest  request,
        CancellationToken ct = default)
    {
        if (!Supports(request.Kind)) return [];

        var mangaId = request.MangaDexId
            ?? await ResolveMangaIdAsync(request.Title, ct);

        if (string.IsNullOrWhiteSpace(mangaId))
        {
            _ctx.Logger.LogDebug("[MangaDex] Could not resolve ID for '{Title}'.", request.Title);
            return [];
        }

        var chapters = await GetChaptersAsync(mangaId, null, ct);

        return chapters.Select(ch =>
        {
            var label = new StringBuilder();
            if (ch.Volume is not null) label.Append($"Vol.{ch.Volume} ");
            label.Append($"Ch.{ch.ChapterNumber ?? "?"}");
            if (ch.Title is not null) label.Append($" — {ch.Title}");

            return new AddonBookResult(
                Format:   AddonBookFormat.WebReader,
                Url:      $"mangadex://chapter/{ch.ChapterId}",
                Language: ch.Language,
                Title:    label.ToString());
        }).ToList();
    }

    /// <summary>
    /// Fetches the full chapter list for a MangaDex manga ID.
    /// Paginates automatically (MangaDex max 100 per request).
    /// Deduplicates by chapter number, preferring the configured language order.
    /// Results are sorted by volume then chapter number.
    /// </summary>
    public async Task<IReadOnlyList<AddonMangaChapter>> GetChaptersAsync(
        string            mangaId,
        string?           language = null,
        CancellationToken ct       = default)
    {
        var langs = language is not null ? [language] : _preferredLanguages;
        var cacheKey = $"chapters:{mangaId}:{string.Join(",", langs)}";

        var cached = await _ctx.GetCachedAsync<List<AddonMangaChapter>>(cacheKey, ct);
        if (cached is not null) return cached;

        var rawChapters = new List<MangaDexChapter>();
        int offset = 0;
        const int limit = 100;

        while (true)
        {
            var url = BuildChapterUrl(mangaId, langs, offset, limit);
            MangaDexChapterResponse? response;

            try
            {
                response = await RetryAsync(
                    () => _http.GetFromJsonAsync<MangaDexChapterResponse>(url, ct), ct);
            }
            catch (Exception ex)
            {
                _ctx.Logger.LogWarning(ex, "[MangaDex] Chapter fetch failed for manga {Id}.", mangaId);
                break;
            }

            if (response?.Data is null || response.Data.Count == 0) break;

            rawChapters.AddRange(response.Data);

            if (rawChapters.Count >= response.Total) break;
            offset += limit;
        }

        var chapters = DeduplicateAndSort(rawChapters, langs);
        await _ctx.SetCachedAsync(cacheKey, chapters, _chapterCacheTtl, ct);

        _ctx.Logger.LogDebug("[MangaDex] Fetched {Count} chapters for manga {Id}.", chapters.Count, mangaId);
        return chapters;
    }

    /// <summary>
    /// Fetches page image URLs for a chapter from the MangaDex at-home server.
    /// Returns full URLs in the format: <c>{baseUrl}/data/{hash}/{filename}</c>.
    /// Uses data-saver images when configured.
    /// </summary>
    public async Task<AddonMangaPages?> GetPagesAsync(
        string            chapterId,
        CancellationToken ct = default)
    {
        var cacheKey = $"pages:{chapterId}";
        var cached = await _ctx.GetCachedAsync<AddonMangaPages>(cacheKey, ct);
        if (cached is not null) return cached;

        MangaDexAtHomeResponse? response;
        try
        {
            response = await RetryAsync(
                () => _http.GetFromJsonAsync<MangaDexAtHomeResponse>($"at-home/server/{chapterId}", ct), ct);
        }
        catch (Exception ex)
        {
            _ctx.Logger.LogWarning(ex, "[MangaDex] Pages fetch failed for chapter {Id}.", chapterId);
            return null;
        }

        if (response?.Chapter is null || string.IsNullOrWhiteSpace(response.BaseUrl)) return null;

        var filenames = _dataSaver && response.Chapter.DataSaver.Count > 0
            ? response.Chapter.DataSaver
            : response.Chapter.Data;

        var qualifier = _dataSaver ? "data-saver" : "data";
        var pages = filenames
            .Select(f => $"{response.BaseUrl}/{qualifier}/{response.Chapter.Hash}/{f}")
            .ToList();

        var result = new AddonMangaPages(pages);
        await _ctx.SetCachedAsync(cacheKey, result, _pagesCacheTtl, ct);

        _ctx.Logger.LogDebug("[MangaDex] Fetched {Count} pages for chapter {Id}.", pages.Count, chapterId);
        return result;
    }

    // ── Resolution helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Searches MangaDex by title and caches the resolved MangaDex ID.
    /// Uses multiple title variants (romaji, english, native) if the title
    /// contains separators like " / " (common in AniList metadata).
    /// </summary>
    private async Task<string?> ResolveMangaIdAsync(string title, CancellationToken ct)
    {
        var normalizedTitle = title.Trim();
        var cacheKey = $"resolve:{normalizedTitle.ToLowerInvariant()}";

        var cached = await _ctx.GetCachedAsync<string>(cacheKey, ct);
        if (cached is not null) return cached;

        // Try primary title first, then each split variant.
        var candidates = BuildTitleCandidates(normalizedTitle);

        foreach (var candidate in candidates)
        {
            var results = await SearchMangaAsync(candidate, ct);
            var bestId  = FindBestMatch(results, candidate);
            if (bestId is null) continue;

            await _ctx.SetCachedAsync(cacheKey, bestId, _searchCacheTtl, ct);
            _ctx.Logger.LogInformation("[MangaDex] Resolved '{Title}' → {Id}.", normalizedTitle, bestId);
            return bestId;
        }

        return null;
    }

    private async Task<IReadOnlyList<MangaDexManga>> SearchMangaAsync(string query, CancellationToken ct)
    {
        var cacheKey = $"search:{query.ToLowerInvariant()}";
        var cached = await _ctx.GetCachedAsync<List<MangaDexManga>>(cacheKey, ct);
        if (cached is not null) return cached;

        MangaDexMangaResponse? response;
        try
        {
            var url = $"manga?title={Uri.EscapeDataString(query)}&limit=10";
            response = await RetryAsync(
                () => _http.GetFromJsonAsync<MangaDexMangaResponse>(url, ct), ct);
        }
        catch (Exception ex)
        {
            _ctx.Logger.LogWarning(ex, "[MangaDex] Search failed for '{Query}'.", query);
            return [];
        }

        var data = response?.Data ?? [];
        await _ctx.SetCachedAsync(cacheKey, data, _searchCacheTtl, ct);
        return data;
    }

    // ── Match scoring ─────────────────────────────────────────────────────────

    private static string? FindBestMatch(IReadOnlyList<MangaDexManga> results, string query)
    {
        if (results.Count == 0) return null;

        var normalized = query.ToLowerInvariant().Trim();

        // 1. Exact match across all title variants.
        foreach (var manga in results)
        {
            if (AllTitles(manga).Any(t => t.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
                return manga.Id;
        }

        // 2. One title starts with the query or vice-versa.
        foreach (var manga in results)
        {
            if (AllTitles(manga).Any(t =>
                    t.StartsWith(normalized, StringComparison.OrdinalIgnoreCase) ||
                    normalized.StartsWith(t, StringComparison.OrdinalIgnoreCase)))
                return manga.Id;
        }

        // 3. Fallback: first result.
        return results[0].Id;
    }

    private static IEnumerable<string> AllTitles(MangaDexManga manga)
    {
        foreach (var kv in manga.Attributes.Title)
            yield return kv.Value;
        foreach (var alt in manga.Attributes.AltTitles)
            foreach (var kv in alt)
                yield return kv.Value;
    }

    /// <summary>
    /// Splits titles like "Romaji / English" or "Native (Alternative)" into candidates.
    /// AniList often returns titles with multiple variants as a single string.
    /// </summary>
    private static IReadOnlyList<string> BuildTitleCandidates(string title)
    {
        var candidates = new List<string> { title };

        // Split on " / " — common AniList separator.
        var parts = title.Split(" / ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
            candidates.AddRange(parts);

        // Remove content in parentheses as an additional variant.
        var withoutParens = System.Text.RegularExpressions.Regex.Replace(title, @"\s*\(.*?\)", "").Trim();
        if (!string.IsNullOrWhiteSpace(withoutParens) && withoutParens != title)
            candidates.Add(withoutParens);

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ── Chapter dedup / sort ──────────────────────────────────────────────────

    private static List<AddonMangaChapter> DeduplicateAndSort(
        List<MangaDexChapter> chapters,
        string[]              preferredLanguages)
    {
        // Group by chapter number (use ID as fallback to preserve one-shots with no number).
        var grouped = chapters.GroupBy(c => c.Attributes.Chapter ?? $"__oneshot_{c.Id}");

        var result = new List<AddonMangaChapter>();

        foreach (var group in grouped)
        {
            // Within each group pick the entry with the highest-priority language.
            MangaDexChapter? best = null;
            int bestLangRank = int.MaxValue;

            foreach (var ch in group)
            {
                var rank = Array.IndexOf(preferredLanguages, ch.Attributes.TranslatedLanguage);
                if (rank == -1) rank = preferredLanguages.Length; // unknown language → last

                if (rank < bestLangRank)
                {
                    bestLangRank = rank;
                    best = ch;
                }
            }

            if (best is null) continue;

            result.Add(new AddonMangaChapter(
                ChapterId:     best.Id,
                ChapterNumber: best.Attributes.Chapter,
                Volume:        best.Attributes.Volume,
                Title:         best.Attributes.Title,
                Language:      best.Attributes.TranslatedLanguage,
                Pages:         best.Attributes.Pages,
                PublishedAt:   best.Attributes.PublishAt));
        }

        return result
            .OrderBy(c => ParseFloat(c.Volume))
            .ThenBy(c => ParseFloat(c.ChapterNumber))
            .ToList();
    }

    // ── URL builders ──────────────────────────────────────────────────────────

    private static string BuildChapterUrl(string mangaId, string[] languages, int offset, int limit)
    {
        var sb = new StringBuilder();
        sb.Append($"chapter?manga={mangaId}&limit={limit}&offset={offset}");
        sb.Append("&order[volume]=asc&order[chapter]=asc");
        foreach (var lang in languages)
            sb.Append($"&translatedLanguage[]={Uri.EscapeDataString(lang)}");
        return sb.ToString();
    }

    // ── Retry ─────────────────────────────────────────────────────────────────

    private static async Task<T?> RetryAsync<T>(
        Func<Task<T?>>    action,
        CancellationToken ct,
        int               maxRetries = 3)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
        }
        return default;
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    private static float ParseFloat(string? s)
    {
        if (float.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var f))
            return f;
        return float.MaxValue; // null volume/chapter sorts last
    }

    private static TimeSpan TryParseMinutes(string? raw, int defaultMinutes)
    {
        if (int.TryParse(raw, out var m) && m > 0)
            return TimeSpan.FromMinutes(m);
        return TimeSpan.FromMinutes(defaultMinutes);
    }
}
