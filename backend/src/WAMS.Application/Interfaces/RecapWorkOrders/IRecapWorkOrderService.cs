namespace WAMS.Application.Interfaces.RecapWorkOrders;

using WAMS.Application.DTOs.RecapWorkOrders;

public interface IRecapWorkOrderService
{
    Task<(List<RecapWorkOrderSummaryResponse> Items, int TotalCount)> GetAllAsync(RecapWorkOrderQuery q, long userId, CancellationToken ct = default);
    Task<RecapWorkOrderDetailResponse> GetByIdAsync(long id, long userId, CancellationToken ct = default);
    Task<RecapWorkOrderDetailResponse> ApproveAsync(long id, long userId, string? reviewerName, CancellationToken ct = default);
    Task<RecapWorkOrderDetailResponse> RejectAsync(long id, long userId, string? reviewerName, string? reason, CancellationToken ct = default);

    IAsyncEnumerable<RecapWorkOrderSummaryResponse> StreamAllAsync(
        RecapWorkOrderQuery q,
        long userId,
        int limit,
        CancellationToken ct = default);
}
