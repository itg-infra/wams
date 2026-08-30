namespace WAMS.Application.Interfaces.BudgetPlans;

using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Enums;

public interface IBudgetPlanRepository
{
    Task<(List<BudgetPlanSummaryResponse> Items, int TotalCount)> GetAllSummaryAsync(
        BudgetPlanStatus? status, 
        BudgetPlanQuery query, 
        IReadOnlyList<long>? warehouseIds, 
        CancellationToken ct = default
    );

    IAsyncEnumerable<BudgetPlanSummaryResponse> StreamAllAsync(
        BudgetPlanStatus? status,
        BudgetPlanQuery query,
        IReadOnlyList<long>? warehouseIds,
        int limit,
        CancellationToken ct = default
    );

    Task<BudgetPlan?> GetByIdDetailReadAsync(long id, CancellationToken ct = default);
    Task<BudgetPlanResponse?> GetByIdProjectionAsync(long id, CancellationToken ct = default);
    Task<long?> GetWarehouseShadowIdAsync(long id, CancellationToken ct = default);
    Task<BudgetPlan?> GetByIdWithItemsAsync(long id, CancellationToken ct = default);
    Task<BudgetPlan?> GetByIdWithItemsAndWorkOrdersAsync(long id, CancellationToken ct = default);
    Task<BudgetPlan?> GetByIdForSubmitAsync(long id, CancellationToken ct = default);
    Task<BpForWoCreateProjection?> GetForWoCreateAsync(long id, CancellationToken ct = default);
    Task<BudgetPlan?> GetByIdForApprovalAsync(long id, CancellationToken ct = default);
    Task<BudgetPlan?> GetSummaryAsync(long id, CancellationToken ct = default);
    Task<List<BudgetPlan>> GetOverdueForReminderAsync(DateTime cutoff, CancellationToken ct = default);
    Task CreateAsync(BudgetPlan plan, CancellationToken ct = default);
    Task UpdateAsync(BudgetPlan plan, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
    Task SetItemsDocExternalAsync(List<long> itemIds, string docExternal, CancellationToken ct = default);
    Task RejectViaRecapAsync(long budgetPlanId, long userId, DateTime rejectedAt, string? reason, CancellationToken ct = default);
}
