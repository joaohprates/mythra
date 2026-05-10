using System.Text.Json.Serialization;

namespace Mythra.Addon.MangaDex.Models;

// ── Manga search / detail ─────────────────────────────────────────────────────

internal sealed class MangaDexMangaResponse
{
    [JsonPropertyName("result")] public string Result { get; set; } = "";
    [JsonPropertyName("data")]   public List<MangaDexManga> Data { get; set; } = [];
    [JsonPropertyName("total")]  public int Total { get; set; }
}

internal sealed class MangaDexManga
{
    [JsonPropertyName("id")]         public string Id { get; set; } = "";
    [JsonPropertyName("attributes")] public MangaDexMangaAttributes Attributes { get; set; } = new();
}

internal sealed class MangaDexMangaAttributes
{
    [JsonPropertyName("title")]       public Dictionary<string, string> Title { get; set; } = [];
    [JsonPropertyName("altTitles")]   public List<Dictionary<string, string>> AltTitles { get; set; } = [];
    [JsonPropertyName("status")]      public string? Status { get; set; }
    [JsonPropertyName("description")] public Dictionary<string, string> Description { get; set; } = [];
}

// ── Chapter list ──────────────────────────────────────────────────────────────

internal sealed class MangaDexChapterResponse
{
    [JsonPropertyName("result")] public string Result { get; set; } = "";
    [JsonPropertyName("data")]   public List<MangaDexChapter> Data { get; set; } = [];
    [JsonPropertyName("total")]  public int Total { get; set; }
}

internal sealed class MangaDexChapter
{
    [JsonPropertyName("id")]         public string Id { get; set; } = "";
    [JsonPropertyName("attributes")] public MangaDexChapterAttributes Attributes { get; set; } = new();
}

internal sealed class MangaDexChapterAttributes
{
    [JsonPropertyName("chapter")]            public string? Chapter { get; set; }
    [JsonPropertyName("volume")]             public string? Volume { get; set; }
    [JsonPropertyName("title")]              public string? Title { get; set; }
    [JsonPropertyName("translatedLanguage")] public string TranslatedLanguage { get; set; } = "en";
    [JsonPropertyName("pages")]              public int Pages { get; set; }
    [JsonPropertyName("publishAt")]          public DateTimeOffset? PublishAt { get; set; }
}

// ── At-home server (page URLs) ────────────────────────────────────────────────

internal sealed class MangaDexAtHomeResponse
{
    [JsonPropertyName("result")]  public string Result { get; set; } = "";
    [JsonPropertyName("baseUrl")] public string BaseUrl { get; set; } = "";
    [JsonPropertyName("chapter")] public MangaDexAtHomeChapter? Chapter { get; set; }
}

internal sealed class MangaDexAtHomeChapter
{
    [JsonPropertyName("hash")]      public string Hash { get; set; } = "";
    [JsonPropertyName("data")]      public List<string> Data { get; set; } = [];
    [JsonPropertyName("dataSaver")] public List<string> DataSaver { get; set; } = [];
}
