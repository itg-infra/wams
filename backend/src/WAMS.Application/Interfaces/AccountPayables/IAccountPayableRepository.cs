namespace WAMS.Application.Interfaces.AccountPayables;

using WAMS.Application.DTOs.AccountPayables;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Entities.BudgetPlans;

public interface IAccountPayableRepository
{
    Task<(List<AccountPayableSummaryResponse> Items, int TotalCount)> GetAllAsync(AccountPayableQuery q, CancellationToken ct = default);
    IAsyncEnumerable<AccountPayableSummaryResponse> StreamAllAsync(AccountPayableQuery q, int limit, CancellationToken ct = default);
    Task<AccountPayable?> GetByIdWithItemsAsync(long id, CancellationToken ct = default);
    Task<List<BudgetPlanItem>> GetAvailableItemsAsync(long vendorShadowId, List<long> budgetPlanItemIds, long? excludeDocumentId = null, List<long>? warehouseIds = null, CancellationToken ct = default);
    Task<List<BudgetPlanItemAvailability>> GetAvailabilityDiagnosticsAsync(long vendorShadowId, List<long> itemIds, List<long>? warehouseIds = null, CancellationToken ct = default);
    Task LockBudgetPlanItemsAsync(List<long> budgetPlanItemIds, CancellationToken ct = default);
    Task<List<AvailableApItemResponse>> GetAvailableItemsByBudgetPlansAsync(long vendorShadowId, List<long> budgetPlanIds, bool includeGenerated = false, long? excludeDocumentId = null, List<long>? warehouseIds = null, CancellationToken ct = default);
    Task<(List<ApprovedRecapApStatusResponse> Items, int Total)> GetApprovedRecapsWithApStatusAsync(long[]? warehouseIds, int page, int limit, CancellationToken ct = default);
    Task CreateAsync(AccountPayable ap, CancellationToken ct = default);
    Task<bool> TryClaimForGenerationAsync(long id, string claimToken, CancellationToken ct = default);
    Task<bool> MarkGeneratedAsync(long id, string claimToken, string sapApNumber, int? sapDocEntry, int? sapApdpDocEntry, long generatedByUserId, CancellationToken ct = default);
    Task ReleaseGenerationClaimAsync(long id, string claimToken, CancellationToken ct = default);
    Task<bool> LockForEditAsync(long id, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default);
}
