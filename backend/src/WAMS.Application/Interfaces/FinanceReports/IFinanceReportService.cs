namespace WAMS.Application.Interfaces.FinanceReports;

using WAMS.Application.Common;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.DTOs.PurchaseOrders;

public interface IFinanceReportService
{
    Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetAllAsync(
        DataTableQuery query, long userId, CancellationToken ct = default);

    Task<FinanceReportDetailResponse> GetDetailAsync(
        long budgetPlanId, long userId, CancellationToken ct = default);

    Task<List<FinanceReportCostDetailResponse>> GetCostDetailsForExportAsync(
        long budgetPlanId, string? workOrderId, long userId, CancellationToken ct = default);
}
