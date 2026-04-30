using FluentAssertions;
using Moq;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Abstractions.Providers;
using Mythra.Application.Services.ExternalProviders;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Tests.ExternalProviders;

public sealed class ExternalProviderServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ExternalProviderService MakeService(
        IMediaItemRepository             repo,
        IEnumerable<IExternalVideoProvider> videoProviders,
        IEnumerable<IExternalBookProvider>? bookProviders = null)
        => new(videoProviders, bookProviders ?? [], repo);

    private static VideoItem MakeVideoItem(string? imdbId = "tt1234567", string title = "Test Movie")
    {
        var item = new VideoItem
        {
            LibraryId       = Guid.NewGuid(),
            Title           = title,
            ProviderImdbId  = imdbId,
        };
        return item;
    }

    // ── GetVideoStreamAsync — not found ────────────────────────────────────────

    [Fact]
    public async Task GetVideoStream_ReturnsNotFound_WhenItemDoesNotExist()
    {
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Media.MediaItem?)null);

        var sut    = MakeService(repo.Object, []);
        var result = await sut.GetVideoStreamAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    // ── GetVideoStreamAsync — fallback ────────────────────────────────────────

    [Fact]
    public async Task GetVideoStream_SkipsFailingProvider_AndReturnsSecondResult()
    {
        var item = MakeVideoItem();
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Provider A (priority 10) — returns null
        var providerA = new Mock<IExternalVideoProvider>();
        providerA.Setup(p => p.Priority).Returns(10);
        providerA.Setup(p => p.Supports(MediaKind.Video)).Returns(true);
        providerA
            .Setup(p => p.GetStreamAsync(It.IsAny<ExternalStreamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalStreamResult?)null);

        // Provider B (priority 20) — returns a result
        var expectedUrl = "https://embed.example.com/movie/tt1234567";
        var providerB   = new Mock<IExternalVideoProvider>();
        providerB.Setup(p => p.Priority).Returns(20);
        providerB.Setup(p => p.Supports(MediaKind.Video)).Returns(true);
        providerB
            .Setup(p => p.GetStreamAsync(It.IsAny<ExternalStreamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalStreamResult("ProviderB", ExternalStreamKind.IframeEmbed, expectedUrl));

        var sut    = MakeService(repo.Object, [providerA.Object, providerB.Object]);
        var result = await sut.GetVideoStreamAsync(item.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Url.Should().Be(expectedUrl);
        result.Value.ProviderName.Should().Be("ProviderB");
        result.Value.StreamKind.Should().Be("IframeEmbed");
    }

    [Fact]
    public async Task GetVideoStream_ReturnsFirstProvider_WithLowestPriority()
    {
        var item = MakeVideoItem();
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        // Priority 5 (should be tried first)
        var highPri = new Mock<IExternalVideoProvider>();
        highPri.Setup(p => p.Priority).Returns(5);
        highPri.Setup(p => p.Supports(MediaKind.Video)).Returns(true);
        highPri
            .Setup(p => p.GetStreamAsync(It.IsAny<ExternalStreamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalStreamResult("First", ExternalStreamKind.IframeEmbed, "https://first.example.com"));

        // Priority 1 (registered later, but lower numeric = higher priority)
        var lowPri = new Mock<IExternalVideoProvider>();
        lowPri.Setup(p => p.Priority).Returns(1);
        lowPri.Setup(p => p.Supports(MediaKind.Video)).Returns(true);
        lowPri
            .Setup(p => p.GetStreamAsync(It.IsAny<ExternalStreamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalStreamResult("ActualFirst", ExternalStreamKind.IframeEmbed, "https://actual-first.example.com"));

        // Pass in reverse priority order — service must sort them
        var sut    = MakeService(repo.Object, [highPri.Object, lowPri.Object]);
        var result = await sut.GetVideoStreamAsync(item.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProviderName.Should().Be("ActualFirst");
    }

    [Fact]
    public async Task GetVideoStream_ReturnsNotFound_WhenNoProviderSucceeds()
    {
        var item = MakeVideoItem();
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var failing = new Mock<IExternalVideoProvider>();
        failing.Setup(p => p.Priority).Returns(10);
        failing.Setup(p => p.Supports(MediaKind.Video)).Returns(true);
        failing
            .Setup(p => p.GetStreamAsync(It.IsAny<ExternalStreamRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalStreamResult?)null);

        var sut    = MakeService(repo.Object, [failing.Object]);
        var result = await sut.GetVideoStreamAsync(item.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetVideoStream_SkipsProvider_WhenSupportsReturnsFalse()
    {
        var item = MakeVideoItem();
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var unsupported = new Mock<IExternalVideoProvider>();
        unsupported.Setup(p => p.Priority).Returns(1);
        // Video item but provider says it doesn't support Video
        unsupported.Setup(p => p.Supports(MediaKind.Video)).Returns(false);

        var sut = MakeService(repo.Object, [unsupported.Object]);
        await sut.GetVideoStreamAsync(item.Id);

        // GetStreamAsync must never be called on an unsupported provider
        unsupported.Verify(
            p => p.GetStreamAsync(It.IsAny<ExternalStreamRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── GetBookLinksAsync — validation ────────────────────────────────────────

    [Fact]
    public async Task GetBookLinks_ReturnsError_WhenItemKindIsVideo()
    {
        var item = MakeVideoItem();
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var sut    = MakeService(repo.Object, []);
        var result = await sut.GetBookLinksAsync(item.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("validation");
    }

    // ── GetBookLinksAsync — aggregation ───────────────────────────────────────

    [Fact]
    public async Task GetBookLinks_AggregatesResultsFromAllProviders()
    {
        var item = new Domain.Media.Books.BookItem
        {
            LibraryId = Guid.NewGuid(),
            Title     = "Pride and Prejudice",
        };
        var repo = new Mock<IMediaItemRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var providerA = new Mock<IExternalBookProvider>();
        providerA.Setup(p => p.Priority).Returns(10);
        providerA.Setup(p => p.Supports(MediaKind.Book)).Returns(true);
        providerA
            .Setup(p => p.GetLinksAsync(It.IsAny<ExternalBookRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalBookResult>
            {
                new("ProviderA", ExternalBookFormat.Epub, "https://a.example.com/book.epub"),
            });

        var providerB = new Mock<IExternalBookProvider>();
        providerB.Setup(p => p.Priority).Returns(20);
        providerB.Setup(p => p.Supports(MediaKind.Book)).Returns(true);
        providerB
            .Setup(p => p.GetLinksAsync(It.IsAny<ExternalBookRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalBookResult>
            {
                new("ProviderB", ExternalBookFormat.PlainText, "https://b.example.com/book.txt"),
            });

        var sut    = new ExternalProviderService([], [providerA.Object, providerB.Object], repo.Object);
        var result = await sut.GetBookLinksAsync(item.Id);

        result.IsSuccess.Should().BeTrue();
        var links = result.Value!;
        links.Should().HaveCount(2);
        links.Select(l => l.ProviderName).Should().BeEquivalentTo(["ProviderA", "ProviderB"]);
    }
}
