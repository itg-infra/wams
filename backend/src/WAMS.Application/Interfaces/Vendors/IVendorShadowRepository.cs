namespace WAMS.Application.Interfaces.Vendors;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Vendors;
using WAMS.Domain.Entities.Vendors;

public interface IVendorShadowRepository
{
    Task<(List<VendorShadow> Items, int TotalCount)> GetAllAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<VendorSummaryResponse> StreamAllAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<VendorShadow?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<VendorShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<VendorShadow> vendors, CancellationToken ct = default);
}
