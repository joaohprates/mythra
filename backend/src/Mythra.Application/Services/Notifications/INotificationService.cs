using Mythra.Domain.Common;
using Mythra.Domain.Notifications;

namespace Mythra.Application.Services.Notifications;

public sealed record NotificationDto(
    Guid Id,
    NotificationKind Kind,
    string Title,
    string? Body,
    string? ActionUrl,
    string? ImageUrl,
    bool IsRead,
    string? Payload,
    DateTimeOffset CreatedAt);

public sealed record NotificationListDto(
    IReadOnlyList<NotificationDto> Items,
    int Total,
    int UnreadCount);

public interface INotificationService
{
    /// <summary>Creates a notification and broadcasts it via SSE to connected clients.</summary>
    Task CreateAsync(Notification notification, CancellationToken ct = default);

    Task<Result<NotificationListDto>> ListAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct = default);
    Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<Result> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<Result> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid userId, Guid notificationId, CancellationToken ct = default);

    /// <summary>Registers an SSE channel for a user. Returns an IAsyncEnumerable that yields events.</summary>
    IAsyncEnumerable<NotificationDto> StreamAsync(Guid userId, CancellationToken ct = default);
}
