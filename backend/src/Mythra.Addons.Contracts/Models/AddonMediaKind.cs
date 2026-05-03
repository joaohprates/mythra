namespace Mythra.Addons.Contracts.Models;

/// <summary>
/// Media kinds supported by the addon system.
/// Mirrors Mythra.Domain.Media.MediaKind but lives in Contracts
/// so addons don't take a dependency on the domain project.
/// </summary>
public enum AddonMediaKind
{
    Movie     = 1,
    Series    = 2,
    Book      = 3,
    Manga     = 4,
    Audiobook = 5,
    Music     = 6,
}
