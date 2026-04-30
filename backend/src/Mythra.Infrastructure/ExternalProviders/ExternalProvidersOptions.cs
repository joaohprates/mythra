namespace Mythra.Infrastructure.ExternalProviders;

public sealed class ExternalProvidersOptions
{
    public const string SectionName = "ExternalProviders";

    // ── Video providers ────────────────────────────────────────────────────

    /// <summary>Videasy multilingual iframe embed (highest priority — has PT audio).</summary>
    public bool   VideasyEnabled { get; set; } = true;
    public string VideasyBaseUrl { get; set; } = "https://player.videasy.net";

    /// <summary>Vidapi.ru iframe embed (English-primary, fallback).</summary>
    public bool   VidapiEnabled { get; set; } = true;
    public string VidapiBaseUrl { get; set; } = "https://vidapi.ru/embed";

    /// <summary>Vidsrc.to iframe embed provider.</summary>
    public bool   VidsrcEnabled { get; set; } = true;
    public string VidsrcBaseUrl { get; set; } = "https://vidsrc.to/embed";

    /// <summary>Consumet HLS provider (GogoAnime, etc.).</summary>
    public bool   ConsumetEnabled { get; set; } = false;
    public string ConsumetBaseUrl { get; set; } = "https://api.consumet.org";

    /// <summary>Archive.org public-domain film provider.</summary>
    public bool   ArchiveOrgEnabled { get; set; } = true;
    public string ArchiveOrgBaseUrl { get; set; } = "https://archive.org";

    // ── Book / Audio / Manga providers ────────────────────────────────────

    /// <summary>Project Gutenberg via GutenDex REST API.</summary>
    public bool   GutendexEnabled { get; set; } = true;
    public string GutendexBaseUrl { get; set; } = "https://gutendex.com";

    /// <summary>LibriVox free audiobooks.</summary>
    public bool   LibriVoxEnabled { get; set; } = true;
    public string LibriVoxBaseUrl { get; set; } = "https://librivox.org/api/feed";

    /// <summary>MangaDex online manga reader.</summary>
    public bool   MangaDexEnabled   { get; set; } = true;
    public string MangaDexBaseUrl   { get; set; } = "https://api.mangadex.org";

    /// <summary>Max concurrent requests to MangaDex (rate-limit: 5 req/s).</summary>
    public int MangaDexRateLimit { get; set; } = 5;
}
