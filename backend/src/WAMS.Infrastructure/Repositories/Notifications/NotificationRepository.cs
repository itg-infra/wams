namespace WAMS.Infrastructure.Repositories.Notifications;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Domain.Entities.Notifications;
using WAMS.Infrastructure.Data;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public Task CreateRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        db.Notifications.AddRange(notifications);
        return Task.CompletedTask;
    }

    public async Task<(List<Notification> Items, int TotalCount)> GetByRecipientAsync(
        long recipientUserId,
        NotificationQuery query,
        CancellationToken ct = default
    )
    {
        var notifications = db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId)
            .AsQueryable();

        if (query.UnreadOnly == true)
            notifications = notifications.Where(n => !n.IsRead);

        notifications = notifications.OrderByDescending(n => n.CreatedAt);

        var total = await notifications.CountAsync(ct);
        var items = await notifications
            .Skip((query.NormalizedPage - 1) * query.NormalizedLimit)
            .Take(query.NormalizedLimit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<Notification?> GetByIdAsync(long id, long recipientUserId, CancellationToken ct = default)
        => db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == recipientUserId, ct);

    public Task<bool> ExistsByTypeAndRecipientAsync(
        string type,
        long recipientUserId,
        DateTime since,
        CancellationToken ct = default
    )
        => db.Notifications.AnyAsync(
            n => n.Type == type && n.RecipientUserId == recipientUserId && n.CreatedAt >= since, ct);

    public Task<int> MarkAllAsReadAsync(long recipientUserId, CancellationToken ct = default)
        => db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
}
