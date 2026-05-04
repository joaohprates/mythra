using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Mythra.Api.Controllers;

/// <summary>
/// Proxies remote poster/backdrop images so the frontend can render them even
/// when the user's ISP blocks the upstream CDN (or just to take advantage of
/// shared caching). Anonymous so login-screen artwork still loads.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/proxy")]
public sealed class ImageProxyController : ControllerBase
{
    private const long MaxEntryBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "image.tmdb.org",
        "s4.anilist.co",
        "books.google.com",
        "coverartarchive.org",
        "covers.openlibrary.org",
        "media.kitsu.io",
        "cdn.myanimelist.net",
        "static.tvmaze.com",
    };

    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ImageProxyController> _log;

    public ImageProxyController(
        IHttpClientFactory httpFactory,
        IMemoryCache cache,
        ILogger<ImageProxyController> log)
    {
        _httpFactory = httpFactory;
        _cache = cache;
        _log = log;
    }

    [HttpGet("image")]
    public async Task<IActionResult> Image([FromQuery] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !AllowedHosts.Contains(uri.Host))
        {
            return BadRequest();
        }

        var cacheKey = $"imgproxy:{uri}";
        if (_cache.TryGetValue(cacheKey, out CachedImage? cached) && cached is not null)
        {
            Response.Headers["Cache-Control"] = "public, max-age=86400, immutable";
            return File(cached.Bytes, cached.ContentType);
        }

        try
        {
            var client = _httpFactory.CreateClient("ImageProxy");
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.TryAddWithoutValidation("Cache-Control", "public, max-age=86400");

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogDebug("ImageProxy upstream {Status} for {Url}", (int)resp.StatusCode, uri);
                return StatusCode(StatusCodes.Status502BadGateway);
            }

            var contentLength = resp.Content.Headers.ContentLength;
            if (contentLength is > MaxEntryBytes)
                return StatusCode(StatusCodes.Status502BadGateway);

            var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
            if (bytes.LongLength > MaxEntryBytes)
                return StatusCode(StatusCodes.Status502BadGateway);

            var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            _cache.Set(cacheKey, new CachedImage(bytes, contentType), new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
                Size = bytes.LongLength,
            });

            Response.Headers["Cache-Control"] = "public, max-age=86400, immutable";
            return File(bytes, contentType);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "ImageProxy fetch failed for {Url}", uri);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private sealed record CachedImage(byte[] Bytes, string ContentType);
}
