namespace WAMS.Application.Interfaces.Notifications;

using System.Threading.Channels;
using WAMS.Application.DTOs.Notifications;

public interface INotificationService
{
    Task PublishAsync(IEnumerable<NotificationCreateRequest> notifications, CancellationToken ct = default);
    Task<(List<NotificationResponse> Items, int TotalCount)> GetMyNotificationsAsync(long userId, NotificationQuery query, CancellationToken ct = default);
    Task MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(long userId, CancellationToken ct = default);
    ChannelReader<NotificationResponse> Subscribe(long userId, CancellationToken ct = default);
}
