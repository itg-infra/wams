namespace WAMS.Application.Common;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public int HeartbeatIntervalSeconds { get; set; } = 30;
}
