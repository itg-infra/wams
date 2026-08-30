namespace WAMS.Infrastructure.Repositories.Spk;

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Spk;
using WAMS.Application.Interfaces.Spk;
using WAMS.Domain.Entities.Spk;
using WAMS.Infrastructure.Data;

public class SpkShadowRepository(AppDbContext db) : ISpkShadowRepository
{
    public async Task<(List<SpkShadow> Items, int TotalCount)> GetAllAsync(
        SpkQuery q,
        IReadOnlyList<string>? whsCodes,
        CancellationToken ct = default
    )
    {
        var query = db.SpkShadows.Where(s => s.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Type))
            query = query.Where(s => s.Type == q.Type);

        if (!string.IsNullOrWhiteSpace(q.DocStatus))
            query = query.Where(s => s.DocStatus == q.DocStatus);

        if (!string.IsNullOrWhiteSpace(q.WhsCode))
            query = query.Where(s => s.WhsCode == q.WhsCode);

        if (whsCodes != null)
            query = query.Where(s => whsCodes.Contains(s.WhsCode));

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(s =>
                EF.Functions.ILike(s.DocNo, pattern, "\\") ||
                EF.Functions.ILike(s.BaseDocNo, pattern, "\\") ||
                EF.Functions.ILike(s.CardName, pattern, "\\") ||
                EF.Functions.ILike(s.ItemCode, pattern, "\\") ||
                EF.Functions.ILike(s.ItemName, pattern, "\\") ||
                EF.Functions.ILike(s.Type, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("docno", true) => query.OrderByDescending(s => s.DocNo),
            ("docno", false) => query.OrderBy(s => s.DocNo),
            ("cardname", true) => query.OrderByDescending(s => s.CardName),
            ("cardname", false) => query.OrderBy(s => s.CardName),
            ("syncedat", true) => query.OrderByDescending(s => s.SyncedAt),
            ("syncedat", false) => query.OrderBy(s => s.SyncedAt),
            _ => query.OrderByDescending(s => s.DocNo),
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.Limit).Take(q.Limit).ToListAsync(ct);
        return (items, total);
    }

    public async IAsyncEnumerable<SpkShadowResponse> StreamAllAsync(
        SpkQuery q,
        IReadOnlyList<string>? whsCodes,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var query = db.SpkShadows.Where(s => s.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Type))
            query = query.Where(s => s.Type == q.Type);

        if (!string.IsNullOrWhiteSpace(q.DocStatus))
            query = query.Where(s => s.DocStatus == q.DocStatus);

        if (!string.IsNullOrWhiteSpace(q.WhsCode))
            query = query.Where(s => s.WhsCode == q.WhsCode);

        if (whsCodes != null)
            query = query.Where(s => whsCodes.Contains(s.WhsCode));

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(s =>
                EF.Functions.ILike(s.DocNo, pattern, "\\") ||
                EF.Functions.ILike(s.BaseDocNo, pattern, "\\") ||
                EF.Functions.ILike(s.CardName, pattern, "\\") ||
                EF.Functions.ILike(s.ItemCode, pattern, "\\") ||
                EF.Functions.ILike(s.ItemName, pattern, "\\") ||
                EF.Functions.ILike(s.Type, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("docno", true) => query.OrderByDescending(s => s.DocNo),
            ("docno", false) => query.OrderBy(s => s.DocNo),
            ("cardname", true) => query.OrderByDescending(s => s.CardName),
            ("cardname", false) => query.OrderBy(s => s.CardName),
            ("syncedat", true) => query.OrderByDescending(s => s.SyncedAt),
            ("syncedat", false) => query.OrderBy(s => s.SyncedAt),
            _ => query.OrderByDescending(s => s.DocNo),
        };

        await foreach (var s in query.AsNoTracking().Take(limit).AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return new SpkShadowResponse(
                s.Id, s.Type, s.DocNo, s.BaseDoc, s.BaseDocNo,
                s.CardCode, s.CardName, s.ItemCode, s.ItemName,
                s.Quantity, s.DeliveryQty, s.UoM, s.PackType,
                s.WhsCode, s.WhsName, s.DocStatus, s.BlNo);
        }
    }

    public async Task<SpkShadow?> GetByIdAsync(
        long id,
        IReadOnlyList<string>? whsCodes,
        CancellationToken ct = default
    )
        => await db.SpkShadows.FirstOrDefaultAsync(
            s => s.Id == id && s.IsActive && (whsCodes == null || whsCodes.Contains(s.WhsCode)), ct);

    public async Task<List<SpkShadow>> GetByIdsAsync(
        IEnumerable<long> ids,
        IReadOnlyList<string>? whsCodes,
        CancellationToken ct = default
    )
        => await db.SpkShadows.Where(
            s => s.IsActive && ids.Contains(s.Id) && (whsCodes == null || whsCodes.Contains(s.WhsCode))).ToListAsync(ct);
}
