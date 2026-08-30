namespace WAMS.Infrastructure.Repositories.TransportOrders;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.TransportOrders;
using WAMS.Application.Interfaces.TransportOrders;
using WAMS.Domain.Entities.TransportOrders;
using WAMS.Infrastructure.Data;

public class TransportOrderShadowRepository(AppDbContext db) : ITransportOrderShadowRepository
{
    public async Task<(List<TransportOrderShadow> Items, int TotalCount)> GetAllAsync(
        TransportOrderQuery q,
        CancellationToken ct = default
    )
    {
        var query = db.TransportOrderShadows.Where(t => t.IsActive).AsQueryable();
        query = await ApplyBudgetPlanLocationFilterAsync(query, q.BudgetPlanId, ct);

        var docStatus = string.IsNullOrWhiteSpace(q.DocStatus) ? "O" : q.DocStatus;
        query = query.Where(t => t.DocStatus == docStatus);

        if (!string.IsNullOrWhiteSpace(q.DocNo))
            query = query.Where(t => t.DocNo == q.DocNo);

        if (!string.IsNullOrWhiteSpace(q.Type))
            query = query.Where(t => t.Type == q.Type);

        if (!string.IsNullOrWhiteSpace(q.WhsCode))
            query = query.Where(t => t.WhsCode == q.WhsCode);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(t =>
                EF.Functions.ILike(t.DocNo, pattern, "\\") ||
                EF.Functions.ILike(t.CardName, pattern, "\\") ||
                EF.Functions.ILike(t.VehicleNo, pattern, "\\") ||
                EF.Functions.ILike(t.BlNo, pattern, "\\") ||
                EF.Functions.ILike(t.ItemName, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("docno", true) => query.OrderByDescending(t => t.DocNo).ThenBy(t => t.Id),
            ("docno", false) => query.OrderBy(t => t.DocNo).ThenBy(t => t.Id),
            ("vehicleno", true) => query.OrderByDescending(t => t.VehicleNo).ThenBy(t => t.Id),
            ("vehicleno", false) => query.OrderBy(t => t.VehicleNo).ThenBy(t => t.Id),
            _ => query.OrderByDescending(t => t.SyncedAt).ThenBy(t => t.DocNo).ThenBy(t => t.Id),
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.Limit).Take(q.Limit).ToListAsync(ct);
        return (items, total);
    }

    public async IAsyncEnumerable<TransportOrderShadowResponse> StreamAllAsync(
        TransportOrderQuery q,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var query = db.TransportOrderShadows.Where(t => t.IsActive).AsQueryable();

        query = await ApplyBudgetPlanLocationFilterAsync(query, q.BudgetPlanId, ct);

        var docStatus = string.IsNullOrWhiteSpace(q.DocStatus) ? "O" : q.DocStatus;

        query = query.Where(t => t.DocStatus == docStatus);

        if (!string.IsNullOrWhiteSpace(q.DocNo))
            query = query.Where(t => t.DocNo == q.DocNo);

        if (!string.IsNullOrWhiteSpace(q.Type))
            query = query.Where(t => t.Type == q.Type);

        if (!string.IsNullOrWhiteSpace(q.WhsCode))
            query = query.Where(t => t.WhsCode == q.WhsCode);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(t =>
                EF.Functions.ILike(t.DocNo, pattern, "\\") ||
                EF.Functions.ILike(t.CardName, pattern, "\\") ||
                EF.Functions.ILike(t.VehicleNo, pattern, "\\") ||
                EF.Functions.ILike(t.BlNo, pattern, "\\") ||
                EF.Functions.ILike(t.ItemName, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("docno", true) => query.OrderByDescending(t => t.DocNo).ThenBy(t => t.Id),
            ("docno", false) => query.OrderBy(t => t.DocNo).ThenBy(t => t.Id),
            ("vehicleno", true) => query.OrderByDescending(t => t.VehicleNo).ThenBy(t => t.Id),
            ("vehicleno", false) => query.OrderBy(t => t.VehicleNo).ThenBy(t => t.Id),
            _ => query.OrderByDescending(t => t.SyncedAt).ThenBy(t => t.DocNo).ThenBy(t => t.Id),
        };

        await foreach (var t in query.AsNoTracking().Take(limit).AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return new TransportOrderShadowResponse(
                t.Id, t.DocNo, t.Type,
                t.CardCode, t.CardName, t.VehicleNo, t.VehicleType,
                t.BlNo, t.ItemCode, t.ItemName, t.Quantity, t.UoM,
                t.WhsCode, t.WhsName, t.DocStatus);
        }
    }

    public async Task<TransportOrderShadow?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.TransportOrderShadows.FirstOrDefaultAsync(t => t.Id == id && t.IsActive, ct);

    public async Task<List<TransportOrderShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await db.TransportOrderShadows
            .Where(t => ids.Contains(t.Id) && t.IsActive)
            .ToListAsync(ct);

    private async Task<IQueryable<TransportOrderShadow>> ApplyBudgetPlanLocationFilterAsync(
        IQueryable<TransportOrderShadow> query,
        long? budgetPlanId,
        CancellationToken ct)
    {
        if (!budgetPlanId.HasValue)
            return query;

        var location = await db.BudgetPlans
            .Where(bp => bp.Id == budgetPlanId.Value && bp.DeletedAt == null)
            .Select(bp => bp.Warehouse.Location)
            .SingleOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(location))
            return query.Where(_ => false);

        var warehouseCodes = db.WarehouseShadows
            .Where(w => w.IsActive && w.Location == location)
            .Select(w => w.Code);

        return query.Where(t => warehouseCodes.Contains(t.WhsCode));
    }
}
