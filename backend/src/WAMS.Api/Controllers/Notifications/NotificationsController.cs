namespace WAMS.Api.Controllers.Notifications;

using WAMS.Api.Controllers.Common;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Domain.Constants;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(
    INotificationService notificationService,
    IValidator<SendTestNotificationRequest> sendTestNotificationValidator,
    IOptions<NotificationOptions> notificationOptions
) : BaseController
{
    private TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(notificationOptions.Value.HeartbeatIntervalSeconds);
    private const int RetryDelayMilliseconds = 5000;

    /// <summary>Gets a paginated list of the current user's notifications.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] NotificationQuery query, CancellationToken ct)
    {
        var userId = GetUserId();
        var (items, total) = await notificationService.GetMyNotificationsAsync(userId, query, ct);
        var meta = new PaginationMeta(
            query.NormalizedPage,
            query.NormalizedLimit,
            total,
            (int)Math.Ceiling(total / (double)query.NormalizedLimit)
        );

        return Ok(new PaginatedResponse<NotificationResponse>(
            true,
            items,
            meta,
            GetRequestId()
        ));
    }

    /// <summary>Marks a notification as read.</summary>
    [HttpPost("{id:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken ct)
    {
        await notificationService.MarkAsReadAsync(id, GetUserId(), ct);

        return NoContent();
    }

    /// <summary>Marks all of the current user's notifications as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var count = await notificationService.MarkAllAsReadAsync(GetUserId(), ct);

        return Ok(new { updatedCount = count });
    }

    /// <summary>Sends a test notification to the current user.</summary>
    [HttpPost("test")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendTestNotification([FromBody] SendTestNotificationRequest request, CancellationToken ct)
    {
        var validation = await sendTestNotificationValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            throw new WAMS.Domain.Exceptions.ValidationException(errors);
        }

        var userId = GetUserId();

        await notificationService.PublishAsync(
        [
            new NotificationCreateRequest(
                GetCompanyId(),
                userId,
                userId,
                request.Type,
                request.Title,
                request.Message,
                request.ReferenceType,
                request.ReferenceId)
        ], ct);

        return Accepted(OkResponse(SuccessMessages.Notification.TestDispatched));
    }

    /// <summary>Streams the current user's notifications as server-sent events.</summary>
    [HttpGet("stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task Stream(CancellationToken ct)
    {
        var userId = GetUserId();
        var reader = notificationService.Subscribe(userId, ct);

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.StartAsync(ct);
        await Response.WriteAsync($"retry: {RetryDelayMilliseconds}\n\n", ct);
        await Response.Body.FlushAsync(ct);
        await WriteEventAsync("connected", new { user_id = userId }, ct);

        using var timer = new PeriodicTimer(HeartbeatInterval);
        var tickTask = timer.WaitForNextTickAsync(ct).AsTask();

        while (!ct.IsCancellationRequested)
        {
            var waitToReadTask = reader.WaitToReadAsync(ct).AsTask();
            var completed = await Task.WhenAny(waitToReadTask, tickTask);

            if (completed == tickTask)
            {
                tickTask = timer.WaitForNextTickAsync(ct).AsTask();
                await WriteEventAsync("heartbeat", new { timestamp = DateTime.UtcNow }, ct);
                continue;
            }

            if (!await waitToReadTask)
                break;

            while (reader.TryRead(out var notification))
            {
                await WriteEventAsync("notification", notification, ct);
            }
        }
    }

    private async Task WriteEventAsync(string eventName, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        await Response.WriteAsync($"event: {eventName}\n", ct);
        await Response.WriteAsync($"data: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
