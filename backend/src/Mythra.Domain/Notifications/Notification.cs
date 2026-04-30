using Mythra.Domain.Common;

namespace Mythra.Domain.Notifications;

public enum NotificationKind
{
    MediaAdded        = 1,
    ScanCompleted     = 2,
    ScanFailed        = 3,
    Recommendation    = 4,
    ImportCompleted   = 5,
    ProviderUnhealthy = 6,
    System            = 99,
}

public sealed class Notification : Entity
{
    /// <summary>Null = broadcast to all users.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Null = not profile-specific.</summary>
    public Guid? ProfileId { get; set; }

    public NotificationKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }

    /// <summary>Client-side route to navigate on click, e.g. "/item/{id}".</summary>
    public string? ActionUrl { get; set; }

    /// <summary>Poster or thumbnail URL for display in notification cards.</summary>
    public string? ImageUrl { get; set; }

    public bool IsRead { get; set; } = false;

    /// <summary>Extra structured data serialised as JSON (mediaItemId, libraryId, etc.).</summary>
    public string? Payload { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    private Notification() { }

    public static Notification Create(
        NotificationKind kind,
        string title,
        string? body = null,
        string? actionUrl = null,
        string? imageUrl = null,
        Guid? userId = null,
        Guid? profileId = null,
        string? payload = null) => new()
        {
            Kind      = kind,
            Title     = title,
            Body      = body,
            ActionUrl = actionUrl,
            ImageUrl  = imageUrl,
            UserId    = userId,
            ProfileId = profileId,
            Payload   = payload,
        };
}
