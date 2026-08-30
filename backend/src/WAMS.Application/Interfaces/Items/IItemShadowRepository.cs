namespace WAMS.Application.Interfaces.Items;

using WAMS.Application.Common;
using WAMS.Application.DTOs.Items;
using WAMS.Domain.Entities.Items;

public interface IItemShadowRepository
{
    Task<(List<ItemShadow> Items, int TotalCount)> GetAllAsync(DataTableQuery query, CancellationToken ct = default);
    IAsyncEnumerable<ItemSummaryResponse> StreamAllAsync(DataTableQuery query, int limit, CancellationToken ct = default);
    Task<ItemShadow?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<ItemShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<ItemShadow> items, CancellationToken ct = default);
    Task<long?> GetIdByItemCodeAsync(string itemCode, CancellationToken ct = default);
}
