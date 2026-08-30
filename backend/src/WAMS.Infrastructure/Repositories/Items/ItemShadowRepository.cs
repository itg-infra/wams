namespace WAMS.Infrastructure.Repositories.Items;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Items;
using WAMS.Application.Interfaces.Items;
using WAMS.Domain.Entities.Items;
using WAMS.Infrastructure.Data;

public class ItemShadowRepository(AppDbContext db) : IItemShadowRepository
{
    public async Task<(List<ItemShadow> Items, int TotalCount)> GetAllAsync(DataTableQuery q, CancellationToken ct = default)
    {
        var query = db.ItemShadows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(i =>
                EF.Functions.ILike(i.ItemCode, pattern, "\\") ||
                EF.Functions.ILike(i.ItemName, pattern, "\\") ||
                EF.Functions.ILike(i.AcctCode, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("itemcode", true) => query.OrderByDescending(i => i.ItemCode),
            ("itemcode", false) => query.OrderBy(i => i.ItemCode),
            ("itemname", true) => query.OrderByDescending(i => i.ItemName),
            ("itemname", false) => query.OrderBy(i => i.ItemName),
            ("acctcode", true) => query.OrderByDescending(i => i.AcctCode),
            ("acctcode", false) => query.OrderBy(i => i.AcctCode),
            ("isactive", true) => query.OrderByDescending(i => i.IsActive),
            ("isactive", false) => query.OrderBy(i => i.IsActive),
            _ => query.OrderBy(i => i.ItemCode),
        };

        var total = await query.CountAsync(ct);
        var items = await query.AsNoTracking().Skip((q.Page - 1) * q.Limit).Take(q.Limit).ToListAsync(ct);
        return (items, total);
    }

    public async IAsyncEnumerable<ItemSummaryResponse> StreamAllAsync(
        DataTableQuery q,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var query = db.ItemShadows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(i =>
                EF.Functions.ILike(i.ItemCode, pattern, "\\") ||
                EF.Functions.ILike(i.ItemName, pattern, "\\") ||
                EF.Functions.ILike(i.AcctCode, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("itemcode", true) => query.OrderByDescending(i => i.ItemCode),
            ("itemcode", false) => query.OrderBy(i => i.ItemCode),
            ("itemname", true) => query.OrderByDescending(i => i.ItemName),
            ("itemname", false) => query.OrderBy(i => i.ItemName),
            ("acctcode", true) => query.OrderByDescending(i => i.AcctCode),
            ("acctcode", false) => query.OrderBy(i => i.AcctCode),
            ("isactive", true) => query.OrderByDescending(i => i.IsActive),
            ("isactive", false) => query.OrderBy(i => i.IsActive),
            _ => query.OrderBy(i => i.ItemCode),
        };

        await foreach (var i in query.AsNoTracking().Take(limit).AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return new ItemSummaryResponse(i.Id, i.ItemCode, i.ItemName, i.AcctCode, i.AcctName);
        }
    }

    public async Task<ItemShadow?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.ItemShadows.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<List<ItemShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await db.ItemShadows.Where(i => ids.Contains(i.Id)).ToListAsync(ct);

    public async Task<long?> GetIdByItemCodeAsync(string itemCode, CancellationToken ct = default)
        => await db.ItemShadows
            .Where(i => i.ItemCode == itemCode)
            .Select(i => (long?)i.Id)
            .FirstOrDefaultAsync(ct);

    public async Task UpsertManyAsync(IEnumerable<ItemShadow> items, CancellationToken ct = default)
    {
        var list = items.ToList();
        var codes = list.Select(i => i.ItemCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await db.ItemShadows
            .Where(i => codes.Contains(i.ItemCode))
            .ToDictionaryAsync(i => i.ItemCode, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var item in list)
        {
            if (existing.TryGetValue(item.ItemCode, out var row))
            {
                row.ItemName = item.ItemName;
                row.AcctCode = item.AcctCode;
                row.AcctName = item.AcctName;
                row.SyncedAt = item.SyncedAt;
                row.IsActive = item.IsActive;
            }
            else
            {
                db.ItemShadows.Add(item);
            }
        }
    }
}
