namespace WAMS.Infrastructure.Repositories.Vendors;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Vendors;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Domain.Entities.Vendors;
using WAMS.Infrastructure.Data;

public class VendorShadowRepository(AppDbContext db) : IVendorShadowRepository
{
    public async Task<(List<VendorShadow> Items, int TotalCount)> GetAllAsync(DataTableQuery q, CancellationToken ct = default)
    {
        var query = db.VendorShadows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(v =>
                EF.Functions.ILike(v.CardCode, pattern, "\\") ||
                EF.Functions.ILike(v.CardName, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("cardcode", true) => query.OrderByDescending(v => v.CardCode),
            ("cardcode", false) => query.OrderBy(v => v.CardCode),
            ("cardname", true) => query.OrderByDescending(v => v.CardName),
            ("cardname", false) => query.OrderBy(v => v.CardName),
            ("isactive", true) => query.OrderByDescending(v => v.IsActive),
            ("isactive", false) => query.OrderBy(v => v.IsActive),
            _ => query.OrderBy(v => v.CardCode),
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.Limit).Take(q.Limit).ToListAsync(ct);
        return (items, total);
    }

    public async IAsyncEnumerable<VendorSummaryResponse> StreamAllAsync(
        DataTableQuery q,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var query = db.VendorShadows.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(v =>
                EF.Functions.ILike(v.CardCode, pattern, "\\") ||
                EF.Functions.ILike(v.CardName, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("cardcode", true) => query.OrderByDescending(v => v.CardCode),
            ("cardcode", false) => query.OrderBy(v => v.CardCode),
            ("cardname", true) => query.OrderByDescending(v => v.CardName),
            ("cardname", false) => query.OrderBy(v => v.CardName),
            ("isactive", true) => query.OrderByDescending(v => v.IsActive),
            ("isactive", false) => query.OrderBy(v => v.IsActive),
            _ => query.OrderBy(v => v.CardCode),
        };

        await foreach (var v in query.AsNoTracking().Take(limit).AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return new VendorSummaryResponse(v.Id, v.CardCode, v.CardName);
        }
    }

    public async Task<VendorShadow?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.VendorShadows.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<List<VendorShadow>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default)
        => await db.VendorShadows.Where(v => ids.Contains(v.Id)).ToListAsync(ct);

    public async Task UpsertManyAsync(IEnumerable<VendorShadow> vendors, CancellationToken ct = default)
    {
        var list = vendors.ToList();
        var codes = list.Select(v => v.CardCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var existing = await db.VendorShadows
            .Where(v => codes.Contains(v.CardCode))
            .ToDictionaryAsync(v => v.CardCode, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var vendor in list)
        {
            if (existing.TryGetValue(vendor.CardCode, out var row))
            {
                row.CardName = vendor.CardName;
                row.SyncedAt = vendor.SyncedAt;
                row.IsActive = vendor.IsActive;
            }
            else
            {
                db.VendorShadows.Add(vendor);
            }
        }
    }
}
