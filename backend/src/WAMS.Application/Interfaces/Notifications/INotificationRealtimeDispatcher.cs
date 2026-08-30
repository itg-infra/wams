namespace WAMS.Application.Interfaces.Notifications;

using System.Threading.Channels;
using WAMS.Application.DTOs.Notifications;

public interface INotificationRealtimeDispatcher
{
    ChannelReader<NotificationResponse> Subscribe(long userId, CancellationToken ct = default);
    Task PublishAsync(NotificationResponse notification, CancellationToken ct = default);
}
