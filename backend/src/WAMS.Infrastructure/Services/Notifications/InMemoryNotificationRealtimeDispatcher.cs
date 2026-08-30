namespace WAMS.Infrastructure.Services.Notifications;

using System.Collections.Concurrent;
using System.Threading.Channels;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.Notifications;

public class InMemoryNotificationRealtimeDispatcher : INotificationRealtimeDispatcher
{
    private const int SubscriptionQueueCapacity = 100;
    private readonly ConcurrentDictionary<long, ConcurrentDictionary<Guid, Channel<NotificationResponse>>> _subscriptions = new();

    public ChannelReader<NotificationResponse> Subscribe(long userId, CancellationToken ct = default)
    {
        // Drop stale events instead of allowing slow clients to grow the queue indefinitely.
        var channel = Channel.CreateBounded<NotificationResponse>(new BoundedChannelOptions(SubscriptionQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        var subscriptionId = Guid.NewGuid();
        var userChannels = _subscriptions.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Channel<NotificationResponse>>());
        userChannels[subscriptionId] = channel;

        ct.Register(() =>
        {
            if (_subscriptions.TryGetValue(userId, out var channels))
            {
                channels.TryRemove(subscriptionId, out _);
                if (channels.IsEmpty)
                    _subscriptions.TryRemove(userId, out _);
            }

            channel.Writer.TryComplete();
        });

        return channel.Reader;
    }

    public Task PublishAsync(NotificationResponse notification, CancellationToken ct = default)
    {
        if (!_subscriptions.TryGetValue(notification.RecipientUserId, out var channels))
            return Task.CompletedTask;

        foreach (var channel in channels.Values)
        {
            channel.Writer.TryWrite(notification);
        }

        return Task.CompletedTask;
    }
}
