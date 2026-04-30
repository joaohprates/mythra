using Mythra.Domain.Media;
using Mythra.Domain.Media.Audio;
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
    DateTimeOffset CreatedAt);

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
    Guid? ParentId = null);

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
    IReadOnlyList<MangaChapterDto> Chapters);

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
    IReadOnlyList<BookChapterDto> Chapters);

public sealed record BookChapterDto(Guid Id, int Order, string Title, string? Anchor, int? StartPage, int? EndPage);

public sealed record AudioItemDto(
    Guid Id,
    Guid LibraryId,
    string Title,
    string? Author,
    string? Narrator,
    string? Series,
    int? SeriesIndex,
    AudioKind AudioKind,
    TimeSpan? Duration,
    string? CoverPath,
    string? Overview,
    IReadOnlyList<AudioChapterDto> Chapters);

public sealed record AudioChapterDto(Guid Id, int Order, string Title, TimeSpan Start, TimeSpan Duration);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);
