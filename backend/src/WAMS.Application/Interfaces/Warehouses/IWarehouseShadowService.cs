namespace WAMS.Application.Interfaces.Warehouses;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Warehouses;

public interface IWarehouseShadowService
{
    Task<PaginatedResponse<WarehouseResponse>> GetAllAsync(long userId, WarehouseQuery query, CancellationToken ct = default);
    IAsyncEnumerable<WarehouseResponse> StreamAllAsync(long userId, WarehouseQuery query, int limit, CancellationToken ct = default);
    Task<WarehouseResponse> GetByIdAsync(long id, long userId, CancellationToken ct = default);
    Task<List<ProvinceOption>> GetDistinctLocationsAsync(long userId, CancellationToken ct = default);
    Task<List<WarehouseResponse>> GetUnmappedAsync(long userId, CancellationToken ct = default);
}
