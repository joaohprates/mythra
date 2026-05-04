using Mythra.Domain.Common;
using Mythra.Domain.Common.Errors;
using Mythra.Domain.Media;

namespace Mythra.Domain.Users;

public sealed class Profile : Entity
{
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public bool IsKidFriendly { get; set; }
    public string Theme { get; set; } = "mythra-dark";

    public List<MediaKind> EnabledMediaKinds { get; set; } = [
        MediaKind.Video, MediaKind.Manga, MediaKind.Book];

    // ── Language preferences ──────────────────────────────────────────────────
    /// <summary>Preferred language for metadata titles and overviews (e.g. "pt-BR", "en", "ja").</summary>
    public string? PreferredContentLanguage { get; set; }

    /// <summary>Default subtitle language code to auto-select in the player (e.g. "pt", "en").</summary>
    public string? PreferredSubtitleLanguage { get; set; }

    /// <summary>Default audio track language code to auto-select in the player (e.g. "pt", "ja").</summary>
    public string? PreferredAudioLanguage { get; set; }

    /// <summary>When true, always show the original title instead of the localised one.</summary>
    public bool ShowOriginalTitle { get; set; } = false;

    /// <summary>When true, adult (+18) content is visible throughout the application for this profile.</summary>
    public bool ShowAdultContent { get; set; } = false;

    private Profile() { }

    public Profile(Guid userId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvariantViolationException("Profile name is required.");
        UserId = userId;
        Name = name.Trim();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new InvariantViolationException("Profile name is required.");
        Name = newName.Trim();
        Touch();
    }

    public void UpdateLanguagePreferences(
        string? contentLanguage,
        string? subtitleLanguage,
        string? audioLanguage,
        bool? showOriginalTitle)
    {
        if (contentLanguage  is not null) PreferredContentLanguage  = contentLanguage;
        if (subtitleLanguage is not null) PreferredSubtitleLanguage = subtitleLanguage;
        if (audioLanguage    is not null) PreferredAudioLanguage    = audioLanguage;
        if (showOriginalTitle.HasValue)   ShowOriginalTitle          = showOriginalTitle.Value;
        Touch();
    }

    public void UpdatePreferences(bool? showAdultContent)
    {
        if (showAdultContent.HasValue) ShowAdultContent = showAdultContent.Value;
        Touch();
    }
}
