namespace Mythra.Domain.Libraries;

public enum LibraryKind
{
    Video     = 1,
    Anime     = 2,
    Manga     = 3,
    Book      = 4,
    General   = 7,   // Universal — auto-detects type by extension
    Image     = 8,   // Photo gallery
}
