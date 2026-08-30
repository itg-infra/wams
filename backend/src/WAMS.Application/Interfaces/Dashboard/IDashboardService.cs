namespace WAMS.Application.Interfaces.Dashboard;

using WAMS.Application.DTOs.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        long userId,
        IReadOnlyList<string> userRoleNames,
        CancellationToken ct = default);

    Task<(List<DashboardActivityResponse> Items, int TotalCount)> GetTodayActivitiesAsync(
        DashboardActivityQuery query,
        long userId,
        CancellationToken ct = default);

    Task<DashboardHistoryResponse> GetHistoryAsync(
        int year,
        int month,
        long userId,
        CancellationToken ct = default);
}
