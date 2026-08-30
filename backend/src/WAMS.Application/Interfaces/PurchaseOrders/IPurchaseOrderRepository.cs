namespace WAMS.Application.Interfaces.PurchaseOrders;

using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.PurchaseOrders;

public interface IPurchaseOrderRepository
{
    Task<(List<PurchaseOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(PurchaseOrderQuery q, CancellationToken ct = default);
    IAsyncEnumerable<PurchaseOrderSummaryResponse> StreamAllAsync(PurchaseOrderQuery q, int limit, CancellationToken ct = default);
    Task<PurchaseOrder?> GetByIdWithItemsAsync(long id, CancellationToken ct = default);
    Task<List<BudgetPlanItem>> GetAvailableItemsAsync(long vendorShadowId, List<long> budgetPlanItemIds, long? excludeDocumentId = null, List<long>? warehouseIds = null, CancellationToken ct = default);
    Task<List<BudgetPlanItemAvailability>> GetAvailabilityDiagnosticsAsync(long vendorShadowId, List<long> itemIds, List<long>? warehouseIds = null, CancellationToken ct = default);
    Task LockBudgetPlanItemsAsync(List<long> budgetPlanItemIds, CancellationToken ct = default);
    Task<(List<AvailablePoItemResponse> Items, int Total)> GetAvailableItemsForPickerAsync(IReadOnlyCollection<long> vendorShadowIds, long? seedBudgetPlanId, DataTableQuery query, bool includeGenerated = false, long? excludeDocumentId = null, List<long>? warehouseIds = null, CancellationToken ct = default);
    Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetApprovedBudgetPlansWithPoStatusAsync(long[]? warehouseIds, DataTableQuery query, CancellationToken ct = default);
    Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetRecapPurchaseOrdersAsync(bool isRfba, long[]? warehouseIds, DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<ApprovedBudgetPlanPoStatusResponse> StreamRecapPurchaseOrdersAsync(bool isRfba, long[]? warehouseIds, DataTableQuery query, int limit, CancellationToken ct = default);
    Task<List<(long BudgetPlanId, long PoId, string PoCode)>> GetPoSummariesByBudgetPlanIdsAsync(List<long> budgetPlanIds, long excludePoId, CancellationToken ct = default);
    /// <summary>
    /// SAP DocEntry + 0-based line index (SortOrder - 1) per BudgetPlanItemId with a Generated PO
    /// line, for populating baseEntry/baseLine on AP Invoice/APDP creation.
    /// </summary>
    Task<Dictionary<long, (int SapDocEntry, int LineIndex)>> GetGeneratedPoLineRefsAsync(List<long> budgetPlanItemIds, CancellationToken ct = default);
    Task CreateAsync(PurchaseOrder po, CancellationToken ct = default);
    Task UpdateAsync(PurchaseOrder po, CancellationToken ct = default);
    Task<bool> MarkGeneratedAsync(long id, string claimToken, string sapPoNumber, int? sapDocEntry, long generatedByUserId, CancellationToken ct = default);
    Task<bool> TryClaimForGenerationAsync(long id, string claimToken, CancellationToken ct = default);
    Task ReleaseGenerationClaimAsync(long id, string claimToken, CancellationToken ct = default);
    Task<bool> LockForEditAsync(long id, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(long id, CancellationToken ct = default);
}
