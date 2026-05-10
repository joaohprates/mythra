using Mythra.Domain.Media;
using Mythra.Domain.Media.Books;
using Mythra.Domain.Media.Manga;
using Mythra.Domain.Media.Video;

namespace Mythra.Application.Dtos.Media;

public sealed record MediaItemDto(
    Guid Id,
    MediaKind Kind,
    Guid LibraryId,
    string Title,
    string? OriginalTitle,
    string? Overview,
    string? Tagline,
    string? PosterPath,
    string? BackdropPath,
    string? ThumbPath,
    DateOnly? ReleaseDate,
    int? Year,
    double? Rating,
    string? Language,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    bool IsAdult = false,
    string? ExternalId = null);

public sealed record VideoItemDto(
    Guid Id,
    Guid LibraryId,
    string Title,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    DateOnly? ReleaseDate,
    int? Year,
    double? Rating,
    VideoKind VideoKind,
    bool IsAnime,
    TimeSpan? Duration,
    int? Width,
    int? Height,
    string ResolutionLabel,
    int? SeasonNumber,
    int? EpisodeNumber,
    string? VideoCodec,
    string? AudioCodec,
    long? Bitrate,
    IReadOnlyList<string> Genres,
    IReadOnlyList<SubtitleDto> Subtitles,
    IReadOnlyList<AudioTrackDto> AudioTracks,
    IReadOnlyList<ChapterMarkerDto> ChapterMarkers,
    /// <summary>True when there is a local media file on disk; false for virtual/external-only items.</summary>
    bool HasFile = true,
    string? ImdbId = null,
    Guid? ParentId = null,
    string Kind = "Video",
    IReadOnlyList<CastMemberDto>? Cast = null);

public sealed record SubtitleDto(Guid Id, string LanguageCode, string? DisplayName, string Format, SubtitleKind Kind, bool IsDefault, bool IsForced);

public sealed record AudioTrackDto(Guid Id, string LanguageCode, string? DisplayName, int StreamIndex, string Codec, int Channels, string ChannelLayout, bool IsDefault, bool IsCommentary);

public sealed record ChapterMarkerDto(Guid Id, ChapterMarkerKind Kind, string? Label, TimeSpan Start, TimeSpan? End, string? ThumbPath);

public sealed record MangaItemDto(
    Guid Id,
    Guid LibraryId,
    string Title,
    string? Author,
    string? Artist,
    string? Status,
    MangaReadingDirection ReadingDirection,
    int? TotalChapters,
    int? TotalVolumes,
    string? PosterPath,
    string? Overview,
    IReadOnlyList<MangaChapterDto> Chapters,
    string Kind = "Manga",
    /// <summary>True when the item has no local files — only metadata from an external provider.</summary>
    bool IsExternal = false,
    /// <summary>True when this item contains adult content (Hentai / explicit genres).</summary>
    bool IsAdult = false,
    /// <summary>Link to the AniList entry, present when imported via AniList.</summary>
    string? AnilistUrl = null,
    /// <summary>Link to MangaDex entry, present when a MangaDex provider ID is known.</summary>
    string? MangaDexUrl = null);

public sealed record MangaChapterDto(Guid Id, int? VolumeNumber, double ChapterNumber, string? Title, int PageCount, string? CoverPath, DateOnly? ReleaseDate);

public sealed record BookItemDto(
    Guid Id,
    Guid LibraryId,
    string Title,
    string? Author,
    string? Publisher,
    string? Isbn,
    string? Series,
    int? SeriesIndex,
    BookFormat Format,
    int? PageCount,
    int? WordCount,
    string? PosterPath,
    string? Overview,
    IReadOnlyList<BookChapterDto> Chapters,
    string Kind = "Book",
    bool IsExternal = false);

public sealed record BookChapterDto(Guid Id, int Order, string Title, string? Anchor, int? StartPage, int? EndPage);

public sealed record CastMemberDto(string Name, string Role, string? Character, int Order, string? PhotoPath);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);
