namespace WAMS.Application.Interfaces.BudgetPlans;

using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Domain.Enums;

public interface IBudgetPlanService
{
    Task<(List<BudgetPlanSummaryResponse> Items, int TotalCount)> GetAllAsync(
        BudgetPlanStatus? status, 
        BudgetPlanQuery query, 
        long userId, 
        CancellationToken ct = default
    );

    IAsyncEnumerable<BudgetPlanSummaryResponse> StreamAllAsync(
        BudgetPlanStatus? status,
        BudgetPlanQuery query,
        long userId,
        int limit,
        CancellationToken ct = default
    );

    Task<BudgetPlanResponse> GetByIdAsync(
        long id,
        long userId,
        CancellationToken ct = default,
        long? vendorShadowId = null);
    Task<BudgetPlanResponse> CreateAsync(long userId, CreateBudgetPlanRequest request, CancellationToken ct = default);
    Task<BudgetPlanResponse> CreateAndSubmitAsync(long userId, CreateBudgetPlanRequest request, CancellationToken ct = default);
    Task<BudgetPlanResponse> UpdateAsync(long id, UpdateBudgetPlanRequest request, long userId, CancellationToken ct = default);
    Task SubmitAsync(long id, long userId, CancellationToken ct = default);
    Task ApproveAsync(long id, long userId, IReadOnlyList<string> userRoles, CancellationToken ct = default);
    Task RejectAsync(long id, long userId, RejectBudgetPlanRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<BudgetPlanSpkItemResponse> AddSpkItemAsync(long planId, AddSpkItemRequest request, long userId, CancellationToken ct = default);
    Task RemoveSpkItemAsync(long planId, long spkItemId, CancellationToken ct = default);
}
