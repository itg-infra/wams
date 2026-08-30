namespace WAMS.Application.DTOs.Notifications;

public record NotificationResponse(
    long Id,
    string Type,
    string Title,
    string Message,
    string ReferenceType,
    string ReferenceId,
    string Status,
    DateTime CreatedAt,
    DateTime? ReadAt,
    long RecipientUserId,
    long? ActorUserId,
    string? Route
);

public record NotificationQuery
{
    public int Page { get; init; } = 1;
    public int Limit { get; init; } = 20;
    public bool? UnreadOnly { get; init; }

    public int NormalizedPage => Page < 1 ? 1 : Page;
    public int NormalizedLimit => Limit < 1 ? 20 : Limit;
}

public record NotificationCreateRequest(
    long CompanyId,
    long RecipientUserId,
    long? ActorUserId,
    string Type,
    string Title,
    string Message,
    string ReferenceType,
    string ReferenceId
);

public record SendTestNotificationRequest(
    string Type,
    string Title,
    string Message,
    string ReferenceType,
    string ReferenceId
);
