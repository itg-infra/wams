namespace WAMS.Application.Interfaces.TransportOrders;

using WAMS.Application.DTOs.TransportOrders;
using WAMS.Domain.Entities.TransportOrders;

public interface ITransportOrderShadowRepository
{
    Task<(List<TransportOrderShadow> Items, int TotalCount)> GetAllAsync(TransportOrderQuery query, CancellationToken ct = default);
    IAsyncEnumerable<TransportOrderShadowResponse> StreamAllAsync(TransportOrderQuery query, int limit, CancellationToken ct = default);
    Task<TransportOrderShadow?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<TransportOrderShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
}
