namespace Mythra.Infrastructure.Metadata;

public sealed class MetadataOptions
{
    public const string SectionName = "Metadata";

    public string? TmdbApiKey { get; set; }
    public string TmdbBaseUrl { get; set; } = "https://api.themoviedb.org/3";
    public string TmdbImageBaseUrl { get; set; } = "https://image.tmdb.org/t/p/original";
    public string AniListBaseUrl { get; set; } = "https://graphql.anilist.co";

    /// <summary>Stremio Cinemeta — open metadata for movies & series (no API key).</summary>
    public string CinemetaBaseUrl { get; set; } = "https://v3-cinemeta.strem.io/";

    // Open Library (replaces Google Books — free, no key required)
    public string OpenLibraryBaseUrl { get; set; } = "https://openlibrary.org/";

    // Spotify (replaces MusicBrainz — requires free developer app credentials)
    public string? SpotifyClientId { get; set; }
    public string? SpotifyClientSecret { get; set; }
}
