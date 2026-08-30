namespace WAMS.Application.Common;

public sealed class BudgetPlanReminderOptions
{
    public const string SectionName = "BudgetPlanReminder";

    public bool Enabled { get; set; } = true;

    /// <summary>How often the scheduler checks for overdue BPs (minutes).</summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>Hours a BP must remain pending before a reminder is sent.</summary>
    public int ThresholdHours { get; set; } = 24;

    /// <summary>Minimum hours between reminder notifications for the same BP.</summary>
    public int CooldownHours { get; set; } = 24;

    /// <summary>
    /// Hour of day (local time) when reminders may start being sent. Default: 9 (9 AM).
    /// Notifications and emails are suppressed outside [ActiveWindowStartHour, ActiveWindowEndHour).
    /// </summary>
    public int ActiveWindowStartHour { get; set; } = 9;

    /// <summary>
    /// Exclusive upper bound of the active window. Default: 17 (5 PM).
    /// </summary>
    public int ActiveWindowEndHour { get; set; } = 17;

    /// <summary>
    /// IANA or Windows timezone ID used to interpret the active window.
    /// Default: Asia/Jakarta (WIB, UTC+7).
    /// </summary>
    public string TimeZoneId { get; set; } = "Asia/Jakarta";
}
