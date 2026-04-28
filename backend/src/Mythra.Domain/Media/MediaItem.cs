using Mythra.Domain.Common;

namespace Mythra.Domain.Media;

public abstract class MediaItem : AggregateRoot
{
    public abstract MediaKind Kind { get; }

    public Guid LibraryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? SortTitle { get; set; }
    public string? Overview { get; set; }
    public string? Tagline { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? ThumbPath { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public int? Year => ReleaseDate?.Year;
    public double? Rating { get; set; }
    public int? RatingCount { get; set; }
    public string? Language { get; set; }
    public string? CountryCode { get; set; }

    public List<Genre> Genres { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<MediaPersonRole> People { get; set; } = [];

    public string? ProviderTmdbId { get; set; }
    public string? ProviderImdbId { get; set; }
    public string? ProviderAnilistId { get; set; }
    public string? ProviderMalId { get; set; }
    public string? ProviderMusicbrainzId { get; set; }
    public string? ProviderGoogleBooksId { get; set; }

    public DateTimeOffset? LastScannedAt { get; set; }
    public DateTimeOffset? LastMetadataRefreshAt { get; set; }

    public void Rename(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            throw new Common.Errors.InvariantViolationException("Title cannot be empty.");
        Title = newTitle.Trim();
        Touch();
    }
}
