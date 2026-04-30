using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;
using Mythra.Infrastructure.ExternalProviders;

namespace Mythra.Infrastructure.Tests.ExternalProviders;

/// <summary>
/// Uses a stub <see cref="HttpMessageHandler"/> to avoid real HTTP calls.
/// </summary>
public sealed class GutenbergProviderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GutenbergProvider MakeProvider(
        string responseJson,
        HttpStatusCode statusCode  = HttpStatusCode.OK,
        bool           enabled     = true,
        string         baseUrl     = "https://gutendex.com")
    {
        var handler = new StubHttpMessageHandler(responseJson, statusCode);
        var http    = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
        var opts    = Options.Create(new ExternalProvidersOptions
        {
            GutendexEnabled = enabled,
            GutendexBaseUrl = baseUrl,
        });
        return new GutenbergProvider(http, opts, NullLogger<GutenbergProvider>.Instance);
    }

    private static ExternalBookRequest BookRequest(string title = "Pride and Prejudice") =>
        new(Guid.NewGuid(), title, MediaKind.Book);

    // ── Supports() ────────────────────────────────────────────────────────────

    [Fact]
    public void Supports_BookKind_WhenEnabled_ReturnsTrue()
    {
        var sut = MakeProvider("{}");
        sut.Supports(MediaKind.Book).Should().BeTrue();
    }

    [Theory]
    [InlineData(MediaKind.Video)]
    [InlineData(MediaKind.Audio)]
    [InlineData(MediaKind.Manga)]
    public void Supports_NonBook_ReturnsFalse(MediaKind kind)
    {
        var sut = MakeProvider("{}");
        sut.Supports(kind).Should().BeFalse();
    }

    // ── Successful search ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetLinksAsync_ReturnsMappedResults_OnSuccess()
    {
        const string json = """
            {
              "count": 1,
              "results": [
                {
                  "title": "Pride and Prejudice",
                  "authors": [{"name": "Austen, Jane"}],
                  "languages": ["en"],
                  "formats": {
                    "application/epub+zip": "https://www.gutenberg.org/ebooks/1342.epub.images",
                    "image/jpeg": "https://www.gutenberg.org/cache/epub/1342/pg1342.cover.medium.jpg"
                  }
                }
              ]
            }
            """;

        var sut    = MakeProvider(json);
        var result = await sut.GetLinksAsync(BookRequest());

        result.Should().HaveCount(1);
        var link = result[0];
        link.ProviderName.Should().Be("Gutenberg");
        link.Format.Should().Be(ExternalBookFormat.Epub);
        link.Url.Should().Contain("1342.epub");
        link.CoverUrl.Should().Contain("pg1342.cover");
        link.Language.Should().Be("en");
        link.Authors.Should().ContainSingle(a => a == "Austen, Jane");
    }

    [Fact]
    public async Task GetLinksAsync_ReturnsPlainText_WhenNoEpub()
    {
        const string json = """
            {
              "results": [
                {
                  "title": "Moby Dick",
                  "authors": [{"name": "Melville, Herman"}],
                  "languages": ["en"],
                  "formats": {
                    "text/plain; charset=utf-8": "https://www.gutenberg.org/files/2701/2701-0.txt"
                  }
                }
              ]
            }
            """;

        var sut    = MakeProvider(json);
        var result = await sut.GetLinksAsync(BookRequest("Moby Dick"));

        result.Should().HaveCount(1);
        result[0].Format.Should().Be(ExternalBookFormat.PlainText);
    }

    [Fact]
    public async Task GetLinksAsync_ReturnsEmpty_WhenResultsAreNull()
    {
        const string json = """{"count": 0, "results": null}""";
        var sut    = MakeProvider(json);
        var result = await sut.GetLinksAsync(BookRequest());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLinksAsync_ReturnsEmpty_WhenDisabled()
    {
        var sut    = MakeProvider("{}", enabled: false);
        var result = await sut.GetLinksAsync(BookRequest());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLinksAsync_ReturnsEmpty_WhenHttpFails()
    {
        var sut    = MakeProvider("{}", HttpStatusCode.InternalServerError);
        var result = await sut.GetLinksAsync(BookRequest());
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLinksAsync_ReturnsEmpty_ForNonBookKind()
    {
        const string json = """{"results": []}""";
        var sut    = MakeProvider(json);
        var req    = new ExternalBookRequest(Guid.NewGuid(), "Dune", MediaKind.Audio);
        var result = await sut.GetLinksAsync(req);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLinksAsync_CappsResultsAtFive()
    {
        // Build a response with 10 books
        var books = Enumerable.Range(1, 10).Select(i => $$"""
            {
              "title": "Book {{i}}",
              "authors": [],
              "languages": ["en"],
              "formats": {"application/epub+zip": "https://example.com/{{i}}.epub"}
            }
            """);
        var json  = $$"""{"results": [{{string.Join(",", books)}}]}""";

        var sut    = MakeProvider(json);
        var result = await sut.GetLinksAsync(BookRequest());

        result.Should().HaveCount(5);
    }

    // ── Infrastructure: stub handler ──────────────────────────────────────────

    private sealed class StubHttpMessageHandler(string body, HttpStatusCode code) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(code)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
