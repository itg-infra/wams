namespace WAMS.Application.Interfaces.PurchaseOrders;

using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;

public interface IPurchaseOrderService
{
    Task<(List<PurchaseOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(PurchaseOrderQuery q, CancellationToken ct = default);
    IAsyncEnumerable<PurchaseOrderSummaryResponse> StreamAllAsync(PurchaseOrderQuery q, int limit, CancellationToken ct = default);
    Task<PurchaseOrderResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(List<AvailablePoItemResponse> Items, int Total)> GetAvailableItemsAsync(long userId, AvailablePoItemQuery query, CancellationToken ct = default);
    Task<(List<AvailablePoItemResponse> Items, int Total)> GetAvailableItemsForEditAsync(long userId, long purchaseOrderId, EditAvailablePoItemQuery query, CancellationToken ct = default);
    Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetApprovedBudgetPlansAsync(long userId, DataTableQuery query, CancellationToken ct = default);
    Task<(List<ApprovedBudgetPlanPoStatusResponse> Items, int Total)> GetRecapAsync(bool isRfba, long userId, DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<ApprovedBudgetPlanPoStatusResponse> StreamRecapAsync(bool isRfba, long userId, DataTableQuery query, int limit, CancellationToken ct = default);
    Task<RecapPurchaseOrderDetailResponse> GetRecapDetailAsync(bool isRfba, long id, CancellationToken ct = default);
    Task<PurchaseOrderResponse> CreateAsync(long userId, CreatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<PurchaseOrderResponse> CreateAndGenerateAsync(long userId, CreatePurchaseOrderRequest request, CancellationToken ct = default);
    Task<PurchaseOrderResponse> UpdateAsync(long id, long userId, UpdatePurchaseOrderRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<PurchaseOrderResponse> GenerateAsync(long id, long userId, CancellationToken ct = default);
}
