using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Notifications;

namespace Mythra.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(MythraDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await db.Notifications.AddAsync(notification, ct);

    public async Task<IReadOnlyList<Notification>> ListForUserAsync(Guid userId, bool unreadOnly, int skip, int take, CancellationToken ct = default)
    {
        var q = db.Notifications
            .Where(n => n.UserId == null || n.UserId == userId);

        if (unreadOnly) q = q.Where(n => !n.IsRead);

        return await q
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
        => await db.Notifications
            .Where(n => (n.UserId == null || n.UserId == userId) && !n.IsRead)
            .CountAsync(ct);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Notifications.FindAsync([id], ct);

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        => await db.Notifications
            .Where(n => (n.UserId == null || n.UserId == userId) && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

    public void Remove(Notification notification)
        => db.Notifications.Remove(notification);
}
