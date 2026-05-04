using Mythra.Application.Dtos.Media;
using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Mapping;

public static class MediaMappings
{
    public static MediaItemDto ToSummary(this MediaItem item) => new(
        item.Id,
        item.Kind,
        item.LibraryId,
        item.Title,
        item.OriginalTitle,
        item.Overview,
        item.Tagline,
        item.PosterPath,
        item.BackdropPath,
        item.ThumbPath,
        item.ReleaseDate,
        item.Year,
        item.Rating,
        item.Language,
        item.Genres.Select(g => g.Name).ToList(),
        item.Tags.Select(t => t.Name).ToList(),
        item.CreatedAt,
        IsAdult: (item.Genres ?? []).Any(g => AdultGenres.Contains(g.Name)));

    public static VideoItemDto ToDetail(this VideoItem v) => new(
        v.Id,
        v.LibraryId,
        v.Title,
        v.Overview,
        v.PosterPath,
        v.BackdropPath,
        v.ReleaseDate,
        v.Year,
        v.Rating,
        v.VideoKind,
        v.IsAnime,
        v.Duration,
        v.Width,
        v.Height,
        v.ResolutionLabel,
        v.SeasonNumber,
        v.EpisodeNumber,
        v.VideoCodec,
        v.AudioCodec,
        v.Bitrate,
        (v.Genres ?? []).Select(g => g.Name).ToList(),
        (v.Subtitles ?? []).Select(s => new SubtitleDto(s.Id, s.LanguageCode, s.DisplayName, s.Format, s.Kind, s.IsDefault, s.IsForced)).ToList(),
        (v.AudioTracks ?? []).Select(a => new AudioTrackDto(a.Id, a.LanguageCode, a.DisplayName, a.StreamIndex, a.Codec, a.Channels, a.ChannelLayout, a.IsDefault, a.IsCommentary)).ToList(),
        (v.ChapterMarkers ?? []).Select(c => new ChapterMarkerDto(c.Id, c.Kind, c.Label, c.Start, c.End, c.ThumbPath)).ToList(),
        HasFile: !string.IsNullOrWhiteSpace(v.FilePath),
        ImdbId: v.ProviderImdbId,
        ParentId: v.ParentId,
        Kind: "Video");

    private static readonly HashSet<string> AdultGenres = new(StringComparer.OrdinalIgnoreCase)
        { "Hentai", "Ecchi", "Adult", "Adult Content", "Pornography", "Eroge" };

    public static MangaItemDto ToDetail(this MangaItem m) => new(
        m.Id,
        m.LibraryId,
        m.Title,
        m.Author,
        m.Artist,
        m.Status,
        m.ReadingDirection,
        m.TotalChapters,
        m.TotalVolumes,
        m.PosterPath,
        m.Overview,
        m.Chapters.OrderBy(c => c.VolumeNumber ?? 0).ThenBy(c => c.ChapterNumber)
            .Select(c => new MangaChapterDto(c.Id, c.VolumeNumber, c.ChapterNumber, c.Title, c.PageCount, c.CoverPath, c.ReleaseDate))
            .ToList(),
        Kind: "Manga",
        IsExternal: string.IsNullOrWhiteSpace(m.RootPath),
        IsAdult: (m.Genres ?? []).Any(g => AdultGenres.Contains(g.Name)),
        AnilistUrl: m.ProviderAnilistId is not null ? $"https://anilist.co/manga/{m.ProviderAnilistId}" : null,
        MangaDexUrl: m.ProviderMangaDexId is not null ? $"https://mangadex.org/title/{m.ProviderMangaDexId}" : null);

    public static BookItemDto ToDetail(this BookItem b) => new(
        b.Id,
        b.LibraryId,
        b.Title,
        b.Author,
        b.Publisher,
        b.Isbn,
        b.Series,
        b.SeriesIndex,
        b.Format,
        b.PageCount,
        b.WordCount,
        b.PosterPath,
        b.Overview,
        b.Chapters.OrderBy(c => c.Order)
            .Select(c => new BookChapterDto(c.Id, c.Order, c.Title, c.Anchor, c.StartPage, c.EndPage))
            .ToList(),
        Kind: "Book",
        IsExternal: string.IsNullOrWhiteSpace(b.FilePath));
}
