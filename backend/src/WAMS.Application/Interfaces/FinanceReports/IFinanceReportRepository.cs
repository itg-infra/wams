namespace WAMS.Application.Interfaces.FinanceReports;

using WAMS.Application.DTOs.FinanceReports;

public interface IFinanceReportRepository
{
    Task<FinanceReportDetailResponse?> GetDetailAsync(
        long budgetPlanId, IReadOnlyList<long>? warehouseIds, CancellationToken ct = default);
}
