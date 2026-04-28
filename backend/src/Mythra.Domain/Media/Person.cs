using Mythra.Domain.Common;

namespace Mythra.Domain.Media;

public sealed class Person : Entity
{
    public string Name { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string? Biography { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? ProviderTmdbId { get; set; }
}

public enum PersonRole
{
    Actor = 1,
    Director = 2,
    Writer = 3,
    Producer = 4,
    Composer = 5,
    Author = 6,
    Illustrator = 7,
    Narrator = 8,
    VoiceActor = 9,
    GuestStar = 10,
}

public sealed class MediaPersonRole : Entity
{
    public Guid MediaItemId { get; set; }
    public Guid PersonId { get; set; }
    public Person? Person { get; set; }
    public PersonRole Role { get; set; }
    public string? Character { get; set; }
    public int Order { get; set; }
}
