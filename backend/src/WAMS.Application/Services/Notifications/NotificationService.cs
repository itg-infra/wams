namespace WAMS.Application.Services.Notifications;

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Notifications;
using WAMS.Domain.Exceptions;

public class NotificationService(
    INotificationRepository notificationRepo,
    INotificationRealtimeDispatcher realtimeDispatcher,
    IUnitOfWork uow,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task PublishAsync(
        IEnumerable<NotificationCreateRequest> notifications,
        CancellationToken ct = default
    )
    {
        var items = notifications
            .GroupBy(n => new
            {
                n.CompanyId,
                n.RecipientUserId,
                n.ActorUserId,
                n.Type,
                n.Title,
                n.Message,
                n.ReferenceType,
                n.ReferenceId
            })
            .Select(g => new Notification
            {
                CompanyId = g.Key.CompanyId,
                RecipientUserId = g.Key.RecipientUserId,
                ActorUserId = g.Key.ActorUserId,
                Type = g.Key.Type,
                Title = g.Key.Title,
                Message = g.Key.Message,
                ReferenceType = g.Key.ReferenceType,
                ReferenceId = g.Key.ReferenceId,
                IsRead = false
            })
            .ToList();

        if (items.Count == 0)
            return;

        await notificationRepo.CreateRangeAsync(items, ct);
        await uow.CommitAsync(ct);

        await Task.WhenAll(items.Select(async item =>
        {
            try
            {
                await realtimeDispatcher.PublishAsync(Map(item), ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to dispatch realtime notification {NotificationId} to user {RecipientUserId}",
                    item.Id,
                    item.RecipientUserId
                );
            }
        }));
    }

    public async Task<(List<NotificationResponse> Items, int TotalCount)> GetMyNotificationsAsync(
        long userId,
        NotificationQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await notificationRepo.GetByRecipientAsync(userId, query, ct);

        return (items.Select(Map).ToList(), total);
    }

    public async Task MarkAsReadAsync(long notificationId, long userId, CancellationToken ct = default)
    {
        var notification = await notificationRepo.GetByIdAsync(notificationId, userId, ct)
            ?? throw new NotFoundException(ErrorMessages.Notification.NotFound(notificationId));

        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        await uow.CommitAsync(ct);
    }

    public Task<int> MarkAllAsReadAsync(long userId, CancellationToken ct = default)
        => notificationRepo.MarkAllAsReadAsync(userId, ct);

    public ChannelReader<NotificationResponse> Subscribe(long userId, CancellationToken ct = default)
        => realtimeDispatcher.Subscribe(userId, ct);

    private static NotificationResponse Map(Notification notification)
        => new(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ReferenceType,
            notification.ReferenceId,
            notification.IsRead ? "read" : "unread",
            notification.CreatedAt,
            notification.ReadAt,
            notification.RecipientUserId,
            notification.ActorUserId,
            NotificationRouteResolver.Resolve(notification.ReferenceType, notification.ReferenceId)
        );
}
