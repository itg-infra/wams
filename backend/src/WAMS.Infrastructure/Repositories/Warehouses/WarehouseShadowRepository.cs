namespace WAMS.Infrastructure.Repositories.Warehouses;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Infrastructure.Data;

public class WarehouseShadowRepository : IWarehouseShadowRepository
{
    private readonly AppDbContext _db;

    public WarehouseShadowRepository(AppDbContext db) => _db = db;

    public async Task<WarehouseShadow?> GetByIdAsync(long id, CancellationToken ct = default)
        => await _db.WarehouseShadows.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<WarehouseShadow?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _db.WarehouseShadows.FirstOrDefaultAsync(w => w.Code == code, ct);

    public async Task<(List<WarehouseShadow> Items, int TotalCount)> GetAllAsync(
        WarehouseQuery q,
        CancellationToken ct = default
    )
    {
        var (query, skip, take) = ApplyFilter(_db.WarehouseShadows.AsQueryable(), q);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).Include(w => w.Province).ToListAsync(ct);
        return (items, total);
    }

    public async Task<(List<WarehouseShadow> Items, int TotalCount)> GetByIdsAsync(
        IEnumerable<long> ids,
        WarehouseQuery q,
        CancellationToken ct = default
    )
    {
        var baseQuery = _db.WarehouseShadows.Where(w => ids.Contains(w.Id));
        var (query, skip, take) = ApplyFilter(baseQuery, q);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).Include(w => w.Province).ToListAsync(ct);
        return (items, total);
    }

    public IAsyncEnumerable<WarehouseResponse> StreamAllAsync(
        WarehouseQuery q,
        int limit,
        CancellationToken ct = default
    )
    {
        var (query, _, _) = ApplyFilter(_db.WarehouseShadows.AsQueryable(), q);
        return query
            .Take(limit)
            .Select(w => new WarehouseResponse(
                w.Id, w.Code, w.Name, w.Location, w.IsActive, w.FirstSeenAt, w.SyncedAt,
                w.ProvinceId,
                w.Province != null ? w.Province.Name : null,
                w.Province != null ? w.Province.Display : null))
            .AsNoTracking()
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<WarehouseResponse> StreamByIdsAsync(
        IEnumerable<long> ids,
        WarehouseQuery q,
        int limit,
        CancellationToken ct = default
    )
    {
        var baseQuery = _db.WarehouseShadows.Where(w => ids.Contains(w.Id));
        var (query, _, _) = ApplyFilter(baseQuery, q);
        return query
            .Take(limit)
            .Select(w => new WarehouseResponse(
                w.Id, w.Code, w.Name, w.Location, w.IsActive, w.FirstSeenAt, w.SyncedAt,
                w.ProvinceId,
                w.Province != null ? w.Province.Name : null,
                w.Province != null ? w.Province.Display : null))
            .AsNoTracking()
            .AsAsyncEnumerable();
    }

    public async Task<List<WarehouseShadow>> GetUnmappedAsync(CancellationToken ct = default)
        => await _db.WarehouseShadows
            .Where(w => w.IsActive && w.ProvinceId == null)
            .OrderBy(w => w.Location)
            .ToListAsync(ct);

    public async Task<List<(long Id, long CompanyId)>> GetCompanyIdsByIdsAsync(
        IEnumerable<long> ids,
        CancellationToken ct = default
    )
        => (await _db.WarehouseShadows
            .Where(w => ids.Contains(w.Id))
            .Select(w => new { w.Id, w.CompanyId })
            .ToListAsync(ct))
            .Select(w => (w.Id, w.CompanyId)).ToList();

    public async Task<List<long>> GetProvinceIdsForWarehousesAsync(
        IEnumerable<long> warehouseIds,
        CancellationToken ct = default
    )
        => await _db.WarehouseShadows
            .Where(w => warehouseIds.Contains(w.Id) && w.ProvinceId != null)
            .Select(w => w.ProvinceId!.Value)
            .Distinct()
            .ToListAsync(ct);

    public async Task<List<string>> GetCodesByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await _db.WarehouseShadows.Where(w => ids.Contains(w.Id)).Select(w => w.Code).ToListAsync(ct);

    private static (IQueryable<WarehouseShadow> Query, int Skip, int Take) ApplyFilter(
        IQueryable<WarehouseShadow> query,
        WarehouseQuery q
    )
    {
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(w =>
                EF.Functions.ILike(w.Code, pattern, "\\") ||
                EF.Functions.ILike(w.Name, pattern, "\\") ||
                (w.Location != null && EF.Functions.ILike(w.Location, pattern, "\\")));
        }

        if (q.ProvinceId.HasValue)
            query = query.Where(w => w.ProvinceId == q.ProvinceId.Value);

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("code", true) => query.OrderByDescending(w => w.Code),
            ("code", false) => query.OrderBy(w => w.Code),
            ("name", true) => query.OrderByDescending(w => w.Name),
            ("name", false) => query.OrderBy(w => w.Name),
            ("location", true) => query.OrderByDescending(w => w.Location),
            ("location", false) => query.OrderBy(w => w.Location),
            ("isactive", true) => query.OrderByDescending(w => w.IsActive),
            ("isactive", false) => query.OrderBy(w => w.IsActive),
            ("syncedat", true) => query.OrderByDescending(w => w.SyncedAt),
            ("syncedat", false) => query.OrderBy(w => w.SyncedAt),
            _ => query.OrderBy(w => w.Code),
        };

        return (query, (q.Page - 1) * q.Limit, q.Limit);
    }
}
