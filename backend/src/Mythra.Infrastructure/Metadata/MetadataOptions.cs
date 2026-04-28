namespace Mythra.Infrastructure.Metadata;

public sealed class MetadataOptions
{
    public const string SectionName = "Metadata";

    public string? TmdbApiKey { get; set; }
    public string TmdbBaseUrl { get; set; } = "https://api.themoviedb.org/3";
    public string TmdbImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/original";
    public string MusicBrainzBaseUrl { get; set; } = "https://musicbrainz.org/ws/2";
    public string MusicBrainzUserAgent { get; set; } = "Mythra/0.1 (https://mythra.local)";
    public string AniListBaseUrl { get; set; } = "https://graphql.anilist.co";
    public string GoogleBooksBaseUrl { get; set; } = "https://www.googleapis.com/books/v1";
    public string? GoogleBooksApiKey { get; set; }
}
