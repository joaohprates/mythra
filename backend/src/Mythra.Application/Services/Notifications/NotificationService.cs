using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Common;
using Mythra.Domain.Notifications;

namespace Mythra.Application.Services.Notifications;

public sealed class NotificationService(
    INotificationRepository repo,
    IUnitOfWork uow,
    ILogger<NotificationService> log) : INotificationService
{
    // userId → list of channels (one per open SSE connection)
    private static readonly ConcurrentDictionary<Guid, List<Channel<NotificationDto>>> _channels = new();
    private static readonly object _lock = new();

    public async Task CreateAsync(Notification notification, CancellationToken ct = default)
    {
        await repo.AddAsync(notification, ct);
        await uow.SaveChangesAsync(ct);

        var dto = ToDto(notification);
        BroadcastToUser(notification.UserId, dto);
        log.LogDebug("Notification created: {Kind} for user {UserId}", notification.Kind, notification.UserId);
    }

    public async Task<Result<NotificationListDto>> ListAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct = default)
    {
        var items = await repo.ListForUserAsync(userId, unreadOnly, skip, take, ct);
        var unread = await repo.CountUnreadAsync(userId, ct);
        return new NotificationListDto(
            items.Select(ToDto).ToList(),
            items.Count,
            unread);
    }

    public async Task<Result<int>> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await repo.CountUnreadAsync(userId, ct);

    public async Task<Result> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var n = await repo.GetByIdAsync(notificationId, ct);
        if (n is null) return Error.NotFound("Notification", notificationId);
        if (n.UserId != userId && n.UserId is not null) return Error.Forbidden("Not your notification.");
        n.IsRead = true;
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        await repo.MarkAllReadAsync(userId, ct);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var n = await repo.GetByIdAsync(notificationId, ct);
        if (n is null) return Error.NotFound("Notification", notificationId);
        if (n.UserId != userId && n.UserId is not null) return Error.Forbidden("Not your notification.");
        repo.Remove(n);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async IAsyncEnumerable<NotificationDto> StreamAsync(Guid userId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<NotificationDto>();
        lock (_lock)
        {
            var list = _channels.GetOrAdd(userId, _ => []);
            list.Add(channel);
        }

        try
        {
            await foreach (var dto in channel.Reader.ReadAllAsync(ct))
                yield return dto;
        }
        finally
        {
            lock (_lock)
            {
                if (_channels.TryGetValue(userId, out var list))
                {
                    list.Remove(channel);
                    if (list.Count == 0) _channels.TryRemove(userId, out _);
                }
            }
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void BroadcastToUser(Guid? userId, NotificationDto dto)
    {
        if (userId is null)
        {
            // broadcast to everyone
            foreach (var (_, channels) in _channels)
                WriteToChannels(channels, dto);
        }
        else if (_channels.TryGetValue(userId.Value, out var channels))
        {
            WriteToChannels(channels, dto);
        }
    }

    private static void WriteToChannels(List<Channel<NotificationDto>> channels, NotificationDto dto)
    {
        lock (_lock)
        {
            foreach (var ch in channels) ch.Writer.TryWrite(dto);
        }
    }

    private static NotificationDto ToDto(Notification n) => new(
        n.Id, n.Kind, n.Title, n.Body, n.ActionUrl, n.ImageUrl, n.IsRead, n.Payload, n.CreatedAt);
}
