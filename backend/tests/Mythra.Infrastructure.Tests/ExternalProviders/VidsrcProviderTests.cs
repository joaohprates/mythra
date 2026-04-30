using FluentAssertions;
using Microsoft.Extensions.Options;
using Mythra.Application.Abstractions.Providers;
using Mythra.Domain.Media;
using Mythra.Infrastructure.ExternalProviders;

namespace Mythra.Infrastructure.Tests.ExternalProviders;

public sealed class VidsrcProviderTests
{
    private static VidsrcProvider MakeProvider(bool enabled = true, string baseUrl = "https://vidsrc.to/embed")
    {
        var opts = Options.Create(new ExternalProvidersOptions
        {
            VidsrcEnabled = enabled,
            VidsrcBaseUrl = baseUrl,
        });
        return new VidsrcProvider(opts);
    }

    // ── Supports() ────────────────────────────────────────────────────────────

    [Fact]
    public void Supports_VideoKind_WhenEnabled_ReturnsTrue()
    {
        var sut = MakeProvider(enabled: true);
        sut.Supports(MediaKind.Video).Should().BeTrue();
    }

    [Fact]
    public void Supports_VideoKind_WhenDisabled_ReturnsFalse()
    {
        var sut = MakeProvider(enabled: false);
        sut.Supports(MediaKind.Video).Should().BeFalse();
    }

    [Theory]
    [InlineData(MediaKind.Book)]
    [InlineData(MediaKind.Audio)]
    [InlineData(MediaKind.Manga)]
    public void Supports_NonVideoKind_ReturnsFalse(MediaKind kind)
    {
        var sut = MakeProvider(enabled: true);
        sut.Supports(kind).Should().BeFalse();
    }

    // ── Movie URL construction ────────────────────────────────────────────────

    [Fact]
    public async Task GetStreamAsync_Movie_WithImdbId_BuildsCorrectUrl()
    {
        var sut = MakeProvider();
        var req = new ExternalStreamRequest(
            MediaItemId: Guid.NewGuid(),
            Title:       "The Dark Knight",
            Kind:        MediaKind.Video,
            ImdbId:      "tt0468569");

        var result = await sut.GetStreamAsync(req);

        result.Should().NotBeNull();
        result!.Url.Should().Be("https://vidsrc.to/embed/movie/tt0468569");
        result.StreamKind.Should().Be(ExternalStreamKind.IframeEmbed);
        result.ProviderName.Should().Be("Vidsrc");
    }

    [Fact]
    public async Task GetStreamAsync_Movie_FallsBackToTmdbId_WhenNoImdbId()
    {
        var sut = MakeProvider();
        var req = new ExternalStreamRequest(
            MediaItemId: Guid.NewGuid(),
            Title:       "Inception",
            Kind:        MediaKind.Video,
            TmdbId:      "27205");

        var result = await sut.GetStreamAsync(req);

        result.Should().NotBeNull();
        result!.Url.Should().Be("https://vidsrc.to/embed/movie/27205");
    }

    [Fact]
    public async Task GetStreamAsync_Movie_ReturnsNull_WhenNoIdentifiers()
    {
        var sut = MakeProvider();
        var req = new ExternalStreamRequest(
            MediaItemId: Guid.NewGuid(),
            Title:       "Unknown",
            Kind:        MediaKind.Video);

        var result = await sut.GetStreamAsync(req);

        result.Should().BeNull();
    }

    // ── TV series URL construction ────────────────────────────────────────────

    [Fact]
    public async Task GetStreamAsync_TvSeries_IncludesSeasonAndEpisode()
    {
        var sut = MakeProvider();
        var req = new ExternalStreamRequest(
            MediaItemId: Guid.NewGuid(),
            Title:       "Breaking Bad",
            Kind:        MediaKind.Video,
            ImdbId:      "tt0903747",
            Season:      2,
            Episode:     5);

        var result = await sut.GetStreamAsync(req);

        result.Should().NotBeNull();
        result!.Url.Should().Be("https://vidsrc.to/embed/tv/tt0903747/2/5");
    }

    [Fact]
    public async Task GetStreamAsync_TvSeries_DefaultsEpisodeToOne_WhenNotProvided()
    {
        var sut = MakeProvider();
        var req = new ExternalStreamRequest(
            MediaItemId: Guid.NewGuid(),
            Title:       "Breaking Bad",
            Kind:        MediaKind.Video,
            ImdbId:      "tt0903747",
            Season:      1);

        var result = await sut.GetStreamAsync(req);

        result!.Url.Should().Be("https://vidsrc.to/embed/tv/tt0903747/1/1");
    }

    // ── Disabled ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStreamAsync_WhenDisabled_ReturnsNull()
    {
        var sut = MakeProvider(enabled: false);
        var req = new ExternalStreamRequest(
            MediaItemId: Guid.NewGuid(),
            Title:       "Some Movie",
            Kind:        MediaKind.Video,
            ImdbId:      "tt1234567");

        var result = await sut.GetStreamAsync(req);

        result.Should().BeNull();
    }

    // ── Metadata ──────────────────────────────────────────────────────────────

    [Fact]
    public void Name_IsVidsrc() => MakeProvider().Name.Should().Be("Vidsrc");

    [Fact]
    public void Priority_IsLowest_AmongVideoProviders() => MakeProvider().Priority.Should().Be(10);

    [Fact]
    public async Task GetStreamAsync_Movie_SetsRefererUrl()
    {
        var sut    = MakeProvider();
        var req    = new ExternalStreamRequest(Guid.NewGuid(), "Movie", MediaKind.Video, ImdbId: "tt1");
        var result = await sut.GetStreamAsync(req);

        result!.RefererUrl.Should().Be("https://vidsrc.to");
    }
}
