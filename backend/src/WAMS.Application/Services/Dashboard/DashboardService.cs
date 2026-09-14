namespace WAMS.Application.Services.Dashboard;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Dashboard;
using WAMS.Application.Interfaces.Dashboard;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;

public class DashboardService(
    IDashboardRepository repo,
    IWarehouseContext warehouseContext,
    IUserRepository userRepo,
    IRbacService rbacService,
    ITenantContext tenantContext
) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(
        long userId,
        IReadOnlyList<string> userRoleNames,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await repo.GetSummaryAsync(GetCompanyId(), warehouseIds, userRoleNames, ct);
    }

    public async Task<(List<DashboardActivityResponse> Items, int TotalCount)> GetTodayActivitiesAsync(
        DashboardActivityQuery query,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await repo.GetTodayActivitiesAsync(query, GetCompanyId(), warehouseIds, ct);
    }

    public async Task<DashboardHistoryResponse> GetHistoryAsync(
        int year,
        int month,
        long userId,
        CancellationToken ct = default
    )
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(userId, ct);

        return await repo.GetHistoryAsync(year, month, GetCompanyId(), warehouseIds, ct);
    }

    private async Task<IReadOnlyList<long>?> ResolveWarehouseIdsAsync(long userId, CancellationToken ct)
    {
        if (warehouseContext.IsSet && warehouseContext.WarehouseId.HasValue)
            return [warehouseContext.WarehouseId.Value];

        var hasGlobal = await UserScope.HasGlobalAccessAsync(rbacService, tenantContext, userId, ct);
        if (!hasGlobal)
            return (await UserScope.GetWarehouseIdsAsync(userRepo, tenantContext, userId, ct)).ToList();

        return null;
    }

    private long GetCompanyId()
        => tenantContext.IsSet && tenantContext.CompanyId.HasValue
            ? tenantContext.CompanyId.Value
            : throw new InvalidOperationException("Dashboard requests require an active company tenant.");
}
