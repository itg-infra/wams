namespace WAMS.Application.Interfaces.WorkOrders;

using WAMS.Application.DTOs.WorkOrders;

public interface IWorkOrderService
{
    Task<(List<WorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(WorkOrderQuery q, long userId, CancellationToken ct = default);

    IAsyncEnumerable<WorkOrderSummaryResponse> StreamAllAsync(
        WorkOrderQuery q,
        long userId,
        int limit,
        CancellationToken ct = default);
    Task<WorkOrderResponse> GetByIdAsync(long id, long userId, CancellationToken ct = default);
    Task<List<WorkOrderPicCandidateResponse>> GetPicCandidatesAsync(long id, long userId, CancellationToken ct = default);
    Task<(List<ApprovedBpForWoResponse> Items, int Total)> GetApprovedBpListAsync(long userId, int page, int limit, CancellationToken ct = default);
    Task BulkCreateDraftAsync(long budgetPlanId, long actorUserId, CancellationToken ct = default);
    Task<WorkOrderResponse> UpdateAsync(long id, UpdateWorkOrderRequest request, long userId, CancellationToken ct = default);
    Task DeleteAsync(long id, long userId, CancellationToken ct = default);
    Task<WorkOrderResponse> SubmitAsync(long id, long userId, CancellationToken ct = default);
}
