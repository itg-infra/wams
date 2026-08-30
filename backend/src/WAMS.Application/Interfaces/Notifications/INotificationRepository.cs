namespace WAMS.Application.Interfaces.Notifications;

using WAMS.Application.DTOs.Notifications;
using WAMS.Domain.Entities.Notifications;

public interface INotificationRepository
{
    Task CreateRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task<(List<Notification> Items, int TotalCount)> GetByRecipientAsync(long recipientUserId, NotificationQuery query, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(long id, long recipientUserId, CancellationToken ct = default);
    Task<bool> ExistsByTypeAndRecipientAsync(string type, long recipientUserId, DateTime since, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(long recipientUserId, CancellationToken ct = default);
}
