using FluentAssertions;
using Moq;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Application.Dtos.Media;
using Mythra.Application.Services.Media;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Audio;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Tests.Media;

public class MediaServiceTests
{
    private readonly Mock<IMediaItemRepository> _media = new();
    private readonly Mock<IVideoRepository> _videos = new();
    private readonly Mock<IMangaRepository> _mangas = new();
    private readonly Mock<IBookRepository> _books = new();
    private readonly Mock<IAudioRepository> _audios = new();
    private readonly Mock<IGenreRepository> _genres = new();

    private MediaService Build() =>
        new(_media.Object, _videos.Object, _mangas.Object, _books.Object, _audios.Object, _genres.Object);

    // ── ListAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_delegates_to_search_and_count_when_no_ids()
    {
        var items = new List<MediaItem> { new VideoItem { Title = "Inception", LibraryId = Guid.NewGuid() } };
        _media.Setup(m => m.SearchAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(items);
        _media.Setup(m => m.CountAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(1);

        var result = await Build().ListAsync(new MediaQuery(Take: 10));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Total.Should().Be(1);
        _media.Verify(m => m.SearchAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        _media.Verify(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_uses_ByIdsAsync_when_ids_provided()
    {
        var items = new List<MediaItem>
        {
            new VideoItem { Title = "Film A", LibraryId = Guid.NewGuid() },
            new VideoItem { Title = "Film B", LibraryId = Guid.NewGuid() },
        };
        var ids = items.Select(i => i.Id).ToList();
        _media.Setup(m => m.ByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(items);

        var result = await Build().ListAsync(new MediaQuery(Ids: ids));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Total.Should().Be(2);
        _media.Verify(m => m.SearchAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        _media.Verify(m => m.CountAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_returns_empty_paged_result_when_library_is_empty()
    {
        _media.Setup(m => m.SearchAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<MediaItem>());
        _media.Setup(m => m.CountAsync(It.IsAny<MediaQuery>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(0);

        var result = await Build().ListAsync(new MediaQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }

    // ── GetDetailAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetDetailAsync_returns_not_found_for_missing_item()
    {
        _media.Setup(m => m.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((MediaItem?)null);

        var result = await Build().GetDetailAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("not_found");
    }

    [Fact]
    public async Task GetDetailAsync_returns_VideoItemDto_for_video()
    {
        var video = new VideoItem { Title = "Interstellar", LibraryId = Guid.NewGuid() };
        _media.Setup(m => m.GetByIdWithDetailsAsync(video.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(video);
        _videos.Setup(v => v.GetByIdWithStreamsAsync(video.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(video);

        var result = await Build().GetDetailAsync(video.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<VideoItemDto>();
        ((VideoItemDto)result.Value!).Title.Should().Be("Interstellar");
    }

    [Fact]
    public async Task GetDetailAsync_returns_MangaItemDto_for_manga()
    {
        var manga = new MangaItem { Title = "Attack on Titan", LibraryId = Guid.NewGuid() };
        _media.Setup(m => m.GetByIdWithDetailsAsync(manga.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(manga);
        _mangas.Setup(mn => mn.GetByIdWithChaptersAsync(manga.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(manga);

        var result = await Build().GetDetailAsync(manga.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<MangaItemDto>();
    }

    [Fact]
    public async Task GetDetailAsync_returns_BookItemDto_for_book()
    {
        var book = new BookItem { Title = "Dune", LibraryId = Guid.NewGuid() };
        _media.Setup(m => m.GetByIdWithDetailsAsync(book.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(book);
        _books.Setup(b => b.GetByIdWithChaptersAsync(book.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(book);

        var result = await Build().GetDetailAsync(book.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<BookItemDto>();
    }

    [Fact]
    public async Task GetDetailAsync_returns_AudioItemDto_for_audio()
    {
        var audio = new AudioItem { Title = "The Hobbit (Audio)", LibraryId = Guid.NewGuid() };
        _media.Setup(m => m.GetByIdWithDetailsAsync(audio.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(audio);
        _audios.Setup(a => a.GetByIdWithChaptersAsync(audio.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(audio);

        var result = await Build().GetDetailAsync(audio.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<AudioItemDto>();
    }

    // ── RecentlyAddedAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RecentlyAddedAsync_returns_mapped_dtos_in_order()
    {
        var lib = Guid.NewGuid();
        var items = new List<MediaItem>
        {
            new VideoItem { Title = "Newest", LibraryId = lib },
            new VideoItem { Title = "Older", LibraryId = lib },
        };
        _media.Setup(m => m.RecentlyAddedAsync(lib, 10, It.IsAny<CancellationToken>()))
              .ReturnsAsync(items);

        var result = await Build().RecentlyAddedAsync(lib, 10);

        result.IsSuccess.Should().BeTrue();
        var value = result.Value!;
        value.Should().HaveCount(2);
        value[0].Title.Should().Be("Newest");
    }

    [Fact]
    public async Task RecentlyAddedAsync_works_with_null_library_id()
    {
        _media.Setup(m => m.RecentlyAddedAsync(null, It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<MediaItem>());

        var result = await Build().RecentlyAddedAsync(null, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    // ── ListGenresAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ListGenresAsync_returns_sorted_distinct_names()
    {
        var genreList = new List<Genre>
        {
            new Genre("Thriller"),
            new Genre("Action"),
            new Genre("Action"),
        };
        _genres.Setup(g => g.ListAsync(null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(genreList);

        var result = await Build().ListGenresAsync(null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeInAscendingOrder();
        result.Value.Should().OnlyHaveUniqueItems();
        result.Value.Should().Contain("Action").And.Contain("Thriller");
    }
}
