namespace WAMS.Application.Interfaces.WorkOrders;

using WAMS.Application.DTOs.WorkOrders;
using WAMS.Domain.Entities.WorkOrders;

public interface IWorkOrderRepository
{
    Task<(List<WorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(
        WorkOrderQuery q, 
        IReadOnlyList<long>? warehouseIds, 
        CancellationToken ct = default
    );

    IAsyncEnumerable<WorkOrderSummaryResponse> StreamAllAsync(
        WorkOrderQuery q,
        IReadOnlyList<long>? warehouseIds,
        int limit,
        CancellationToken ct = default);

    Task<long?> GetWarehouseShadowIdAsync(long id, CancellationToken ct = default);
    Task<WorkOrderResponse?> GetByIdProjectionAsync(long id, CancellationToken ct = default);
    Task<WorkOrder?> GetByIdForUpdateAsync(long id, CancellationToken ct = default);

    Task<(List<ApprovedBpForWoResponse> Items, int Total)> GetApprovedBpListAsync(IReadOnlyList<long>? warehouseIds, int page, int limit, CancellationToken ct = default);
    Task BulkInsertAsync(IReadOnlyList<WorkOrder> workOrders, CancellationToken ct = default);
    Task SubmitAsync(long id, long submittedByUserId, DateTime submittedAt, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);

    Task<WorkOrderAttachmentContext?> GetForAttachmentAsync(long id, CancellationToken ct = default);

    Task<WorkOrderPicContext?> GetPicContextAsync(long id, CancellationToken ct = default);
    Task<bool> HasActiveWorkOrderForItemAsync(long budgetPlanItemId, CancellationToken ct = default);
}

public record WorkOrderAttachmentContext(long Id, long CompanyId, long CreatedByUserId, bool CanBeEdited, long WarehouseShadowId);

public record WorkOrderPicContext(long CompanyId, long WarehouseShadowId);
