namespace WAMS.Application.Services.FinanceReports;

using WAMS.Application.Common;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Interfaces.FinanceReports;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

public class FinanceReportService(
    IFinanceReportRepository repo,
    IPurchaseOrderService poService,
    IWarehouseContext warehouseContext,
    IUserRepository userRepo,
    IRbacService rbacService,
    IWarehouseShadowRepository warehouseRepo,
    ITenantContext? tenantContext = null
) : IFinanceReportService
{
    public async Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetAllAsync(
        DataTableQuery query,
        long userId,
        CancellationToken ct = default
    )
        => await poService.GetApprovedBudgetPlansAsync(userId, query, ct);

    public async Task<FinanceReportDetailResponse> GetDetailAsync(
        long budgetPlanId,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);
        var detail = await repo.GetDetailAsync(budgetPlanId, warehouseIds, ct);

        return detail ?? throw new NotFoundException("BudgetPlan", budgetPlanId);
    }

    public async Task<List<FinanceReportCostDetailResponse>> GetCostDetailsForExportAsync(
        long budgetPlanId,
        string? workOrderId,
        long userId,
        CancellationToken ct = default
    )
    {
        var detail = await GetDetailAsync(budgetPlanId, userId, ct);

        return string.IsNullOrWhiteSpace(workOrderId)
            ? detail.CostDetails
            : [.. detail.CostDetails.Where(x => x.WorkOrderId == workOrderId)];
    }

    private async Task<IReadOnlyList<long>?> ResolveWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
        {
            await EnsureWarehouseAccessAsync(userId, warehouseContext.WarehouseId.Value, ct);
            return [warehouseContext.WarehouseId.Value];
        }

        var hasGlobal = await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct);

        if (!hasGlobal)
            return (await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct)).ToList();

        return null;
    }

    private async Task EnsureWarehouseAccessAsync(long userId, long warehouseId, CancellationToken ct)
    {
        _ = await warehouseRepo.GetByIdAsync(warehouseId, ct)
            ?? throw new NotFoundException(ErrorMessages.Warehouse.NotFound(warehouseId));

        if (await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct)) return;

        var ids = await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct);

        if (!ids.Contains(warehouseId))
            throw new ForbiddenException(ErrorMessages.Warehouse.AccessDenied);
    }
}
