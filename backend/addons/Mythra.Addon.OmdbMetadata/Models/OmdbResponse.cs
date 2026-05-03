using System.Text.Json.Serialization;

namespace Mythra.Addon.OmdbMetadata.Models;

// OMDb API returns a flat object for single-item lookups and a paged list for searches.
// All fields are strings (including "N/A" for missing values).

internal sealed class OmdbDetailResponse
{
    [JsonPropertyName("imdbID")]     public string? ImdbId      { get; init; }
    [JsonPropertyName("Title")]      public string? Title       { get; init; }
    [JsonPropertyName("Year")]       public string? Year        { get; init; }
    [JsonPropertyName("Released")]   public string? Released    { get; init; }
    [JsonPropertyName("Plot")]       public string? Plot        { get; init; }
    [JsonPropertyName("Poster")]     public string? Poster      { get; init; }
    [JsonPropertyName("Genre")]      public string? Genre       { get; init; }
    [JsonPropertyName("Director")]   public string? Director    { get; init; }
    [JsonPropertyName("Actors")]     public string? Actors      { get; init; }
    [JsonPropertyName("Language")]   public string? Language    { get; init; }
    [JsonPropertyName("Country")]    public string? Country     { get; init; }
    [JsonPropertyName("Awards")]     public string? Awards      { get; init; }
    [JsonPropertyName("imdbRating")] public string? ImdbRating  { get; init; }
    [JsonPropertyName("imdbVotes")]  public string? ImdbVotes   { get; init; }
    [JsonPropertyName("Type")]       public string? Type        { get; init; }  // "movie" | "series" | "episode"
    [JsonPropertyName("Rated")]      public string? Rated       { get; init; }  // MPAA rating
    [JsonPropertyName("Runtime")]    public string? Runtime     { get; init; }
    [JsonPropertyName("Response")]   public string? Response    { get; init; }  // "True" | "False"
    [JsonPropertyName("Error")]      public string? Error       { get; init; }

    public bool IsSuccess => string.Equals(Response, "True", StringComparison.OrdinalIgnoreCase);

    // OMDb returns "N/A" for any field it doesn't have — normalize these to null.
    public string? SafeTitle    => NullIfNa(Title);
    public string? SafePlot     => NullIfNa(Plot);
    public string? SafePoster   => NullIfNa(Poster);
    public string? SafeGenre    => NullIfNa(Genre);
    public string? SafeReleased => NullIfNa(Released);
    public string? SafeRating   => NullIfNa(ImdbRating);
    public string? SafeVotes    => NullIfNa(ImdbVotes);

    private static string? NullIfNa(string? s) =>
        string.IsNullOrWhiteSpace(s) || s.Equals("N/A", StringComparison.OrdinalIgnoreCase)
            ? null : s;
}

internal sealed class OmdbSearchResponse
{
    [JsonPropertyName("Search")]      public List<OmdbSearchEntry>? Search      { get; init; }
    [JsonPropertyName("totalResults")] public string? TotalResults { get; init; }
    [JsonPropertyName("Response")]    public string? Response                  { get; init; }
    [JsonPropertyName("Error")]       public string? Error                     { get; init; }

    public bool IsSuccess => string.Equals(Response, "True", StringComparison.OrdinalIgnoreCase);
}

internal sealed class OmdbSearchEntry
{
    [JsonPropertyName("imdbID")]  public string? ImdbId { get; init; }
    [JsonPropertyName("Title")]   public string? Title  { get; init; }
    [JsonPropertyName("Year")]    public string? Year   { get; init; }
    [JsonPropertyName("Poster")]  public string? Poster { get; init; }
    [JsonPropertyName("Type")]    public string? Type   { get; init; }
}
