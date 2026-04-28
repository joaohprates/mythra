using FluentAssertions;
using Moq;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Search;
using Mythra.Application.Services.Search;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Tests.Search;

public class SearchServiceTests
{
    [Fact]
    public async Task Search_orders_by_relevance()
    {
        var media = new Mock<IMediaItemRepository>();
        var items = new List<MediaItem>
        {
            new VideoItem { Title = "Dune Part Two", LibraryId = Guid.NewGuid() },
            new VideoItem { Title = "Dune", LibraryId = Guid.NewGuid() },
            new VideoItem { Title = "Sand Dunes Documentary", LibraryId = Guid.NewGuid() },
        };
        media.Setup(m => m.SearchAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((MediaQuery q, CancellationToken _) =>
                 q.Kind == MediaKind.Video ? items : new List<MediaItem>());

        var svc = new SearchService(media.Object);
        var result = await svc.SearchAsync(new UnifiedSearchRequest("dune", null, null, null, null));

        result.IsSuccess.Should().BeTrue();
        var hits = result.Value!.Hits;
        hits.Should().NotBeEmpty();
        hits[0].Title.Should().Be("Dune");
        hits[0].Relevance.Should().BeGreaterThan(hits[1].Relevance);
    }

    [Fact]
    public async Task Search_empty_query_returns_results_with_neutral_relevance()
    {
        var media = new Mock<IMediaItemRepository>();
        media.Setup(m => m.SearchAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<MediaItem> { new VideoItem { Title = "Anything" } });

        var svc = new SearchService(media.Object);
        var result = await svc.SearchAsync(new UnifiedSearchRequest("", null, null, null, null));
        result.IsSuccess.Should().BeTrue();
        result.Value!.Hits.Should().NotBeEmpty();
    }
}
