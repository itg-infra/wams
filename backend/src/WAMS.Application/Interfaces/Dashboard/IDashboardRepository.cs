namespace WAMS.Application.Interfaces.Dashboard;

using WAMS.Application.DTOs.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardSummaryResponse> GetSummaryAsync(
        IReadOnlyList<long>? warehouseIds,
        IReadOnlyList<string> userRoleNames,
        CancellationToken ct = default);

    Task<(List<DashboardActivityResponse> Items, int TotalCount)> GetTodayActivitiesAsync(
        DashboardActivityQuery query,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default);

    Task<DashboardHistoryResponse> GetHistoryAsync(
        int year,
        int month,
        IReadOnlyList<long>? warehouseIds,
        CancellationToken ct = default);
}
