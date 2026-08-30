namespace WAMS.Application.DTOs.Dashboard;

using WAMS.Application.Common;

public record DashboardActivityQuery : DataTableQuery;

public record DashboardSummaryResponse(
    decimal BudgetAchievedPercent,
    decimal TotalBudgetValue,
    decimal TotalActualValue,
    int ActivePoWithoutApCount,
    int NewPoWithoutApLast7DaysCount,
    int OpenWorkOrderCount,
    int ActiveWarehouseCount,
    int PendingApprovalCount,
    int OverdueApprovalCount);

public record DashboardActivityResponse(
    long BudgetPlanId,
    string BudgetNo,
    string? VendorName,
    string? Remark,
    bool AnyRfba,
    string? Location,
    DateTime Date,
    string Status,
    string StatusDisplay);

public record DashboardHistoryResponse(
    List<DashboardCalendarDay> CalendarDays,
    List<DashboardEventEntry> RecentEvents);

public record DashboardCalendarDay(
    DateOnly Date,
    int EventCount);

public record DashboardEventEntry(
    DateTime OccurredAt,
    string EventType,
    string ActivityTypeName,
    string WarehouseCode);
