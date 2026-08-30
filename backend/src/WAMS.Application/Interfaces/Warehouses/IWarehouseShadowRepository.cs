namespace WAMS.Application.Interfaces.Warehouses;

using WAMS.Application.DTOs.Warehouses;
using WAMS.Domain.Entities.Warehouses;

public interface IWarehouseShadowRepository
{
    Task<WarehouseShadow?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<WarehouseShadow?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<(List<WarehouseShadow> Items, int TotalCount)> GetAllAsync(WarehouseQuery query, CancellationToken ct = default);
    Task<(List<WarehouseShadow> Items, int TotalCount)> GetByIdsAsync(IEnumerable<long> ids, WarehouseQuery query, CancellationToken ct = default);
    IAsyncEnumerable<WarehouseResponse> StreamAllAsync(WarehouseQuery query, int limit, CancellationToken ct = default);
    IAsyncEnumerable<WarehouseResponse> StreamByIdsAsync(IEnumerable<long> ids, WarehouseQuery query, int limit, CancellationToken ct = default);
    Task<List<WarehouseShadow>> GetUnmappedAsync(CancellationToken ct = default);

    Task<List<(long Id, long CompanyId)>> GetCompanyIdsByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<List<long>> GetProvinceIdsForWarehousesAsync(IEnumerable<long> warehouseIds, CancellationToken ct = default);
    Task<List<string>> GetCodesByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
}
