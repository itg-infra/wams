namespace WAMS.Infrastructure.Repositories.RateCards;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.DTOs.Vendors;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class RateCardRepository(AppDbContext db) : IRateCardRepository
{
    public async Task<(List<RateCardSummaryResponse> Items, int TotalCount)> GetAllAsync(
        RateCardStatus? status,
        long? vendorShadowId,
        DataTableQuery q,
        CancellationToken ct = default
    )
    {
        var query = db.RateCards
            .Where(r => r.DeletedAt == null)
            .AsQueryable();

        if (status is not null)
            query = query.Where(r => r.Status == status);

        if (vendorShadowId.HasValue)
            query = query.Where(r => r.VendorShadowId == vendorShadowId.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(r => EF.Functions.ILike(r.Vendor.CardName, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("status", true) => query.OrderByDescending(r => r.Status),
            ("status", false) => query.OrderBy(r => r.Status),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("submittedat", true) => query.OrderByDescending(r => r.SubmittedAt),
            ("submittedat", false) => query.OrderBy(r => r.SubmittedAt),
            _ => query.OrderByDescending(r => r.CreatedAt),
        };

        var total = await query.CountAsync(ct);

        // Project into an anonymous shape so EF translates r.Items.Count() to a
        // SQL COUNT subquery - avoids loading the items collection entirely.
        var rows = await query
            .AsNoTracking()
            .Skip((q.Page - 1) * q.Limit)
            .Take(q.Limit)
            .Select(r => new
            {
                r.Id,
                VendorId = r.Vendor.Id,
                r.Vendor.CardCode,
                r.Vendor.CardName,
                r.Status,
                ItemCount = r.Items.Count(),
                r.CreatedAt,
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new RateCardSummaryResponse(
            r.Id,
            new VendorSummaryResponse(r.VendorId, r.CardCode, r.CardName),
            r.Status.ToString(),
            r.ItemCount,
            r.CreatedAt)).ToList();

        return (items, total);
    }

    public IAsyncEnumerable<RateCardSummaryResponse> StreamAllAsync(
        RateCardStatus? status,
        long? vendorShadowId,
        DataTableQuery q,
        int limit,
        CancellationToken ct = default
    )
    {
        var query = db.RateCards
            .Where(r => r.DeletedAt == null)
            .AsQueryable();

        if (status is not null)
            query = query.Where(r => r.Status == status);

        if (vendorShadowId.HasValue)
            query = query.Where(r => r.VendorShadowId == vendorShadowId.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(r => EF.Functions.ILike(r.Vendor.CardName, pattern, "\\"));
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("status", true) => query.OrderByDescending(r => r.Status),
            ("status", false) => query.OrderBy(r => r.Status),
            ("createdat", true) => query.OrderByDescending(r => r.CreatedAt),
            ("createdat", false) => query.OrderBy(r => r.CreatedAt),
            ("submittedat", true) => query.OrderByDescending(r => r.SubmittedAt),
            ("submittedat", false) => query.OrderBy(r => r.SubmittedAt),
            _ => query.OrderByDescending(r => r.CreatedAt),
        };

        return query
            .AsNoTracking()
            .Take(limit)
            .Select(r => new RateCardSummaryResponse(
                r.Id,
                new VendorSummaryResponse(r.Vendor.Id, r.Vendor.CardCode, r.Vendor.CardName),
                r.Status.ToString(),
                r.Items.Count(),
                r.CreatedAt))
            .AsAsyncEnumerable();
    }

    // Mandatory re-fetch-after-write in RateCardService: the write paths set only scalar columns on
    // RateCardItem (ids, tax code/rate snapshots), not navigation props, so Vendor/Item/Uom must be
    // re-included here to build the response DTO. Tax needs no join - code and rate live on the row.
    public async Task<RateCard?> GetByIdWithItemsAsync(long id, CancellationToken ct = default)
        => await db.RateCards
            .Where(r => r.DeletedAt == null)
            .Include(r => r.Vendor)
            .Include(r => r.Items)
                .ThenInclude(i => i.Item)
            .Include(r => r.Items)
                .ThenInclude(i => i.Uom)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<RateCardItem?> FindSubmittedRateAsync(
        long vendorShadowId,
        long itemShadowId,
        CancellationToken ct = default
    )
        => await db.RateCardItems
            .Include(i => i.Uom)
            .Where(i =>
                i.RateCard.DeletedAt == null &&
                i.RateCard.Status == RateCardStatus.Submitted &&
                i.RateCard.VendorShadowId == vendorShadowId &&
                i.ItemShadowId == itemShadowId)
            .OrderByDescending(i => i.RateCard.SubmittedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Dictionary<(long VendorShadowId, long ItemShadowId), RateCardItem>> FindSubmittedRatesBatchAsync(
        IReadOnlyList<(long VendorShadowId, long ItemShadowId)> pairs,
        CancellationToken ct = default
    )
    {
        var vendorIds = pairs.Select(p => p.VendorShadowId).Distinct().ToList();
        var itemIds = pairs.Select(p => p.ItemShadowId).Distinct().ToList();

        var candidates = await db.RateCardItems
            .Include(i => i.Uom)
            .Include(i => i.RateCard)
            .Where(i =>
                i.RateCard.DeletedAt == null &&
                i.RateCard.Status == RateCardStatus.Submitted &&
                vendorIds.Contains(i.RateCard.VendorShadowId) &&
                itemIds.Contains(i.ItemShadowId))
            .OrderByDescending(i => i.RateCard.SubmittedAt)
            .AsNoTracking()
            .ToListAsync(ct);

        // One entry per (vendor, item) pair - latest submitted rate wins.
        return candidates
            .GroupBy(i => (i.RateCard.VendorShadowId, i.ItemShadowId))
            .ToDictionary(g => g.Key, g => g.First());
    }

    // Diagnoses why a (vendor, item) pair is missing from FindSubmittedRatesBatchAsync's result:
    // no rate card item exists for the pair at all, vs. one exists but its rate card isn't
    // Submitted yet (still Draft). Only called on the failure path (small pair count).
    public async Task<List<RateAvailability>> GetRateAvailabilityDiagnosticsAsync(
        IReadOnlyList<(long VendorShadowId, long ItemShadowId)> pairs,
        CancellationToken ct = default
    )
    {
        var vendorIds = pairs.Select(p => p.VendorShadowId).Distinct().ToList();
        var itemIds = pairs.Select(p => p.ItemShadowId).Distinct().ToList();

        var rows = await db.RateCardItems
            .Where(i =>
                i.RateCard.DeletedAt == null &&
                vendorIds.Contains(i.RateCard.VendorShadowId) &&
                itemIds.Contains(i.ItemShadowId))
            .Select(i => new
            {
                VendorShadowId = i.RateCard.VendorShadowId,
                i.ItemShadowId,
                Submitted = i.RateCard.Status == RateCardStatus.Submitted,
            })
            .ToListAsync(ct);

        return pairs
            .Select(p =>
            {
                var matches = rows.Where(r => r.VendorShadowId == p.VendorShadowId && r.ItemShadowId == p.ItemShadowId).ToList();
                return new RateAvailability(
                    p.VendorShadowId,
                    p.ItemShadowId,
                    Found: matches.Count > 0,
                    Submitted: matches.Any(m => m.Submitted));
            })
            .ToList();
    }

    public async Task<List<RateCardItem>> GetSubmittedRatesForItemAsync(
        long itemShadowId,
        CancellationToken ct = default
    )
    {
        // Two-pass: first get one ID per vendor (latest submitted), then fetch full rows.
        // GroupBy+First in EF translates cleanly; the IDs list stays small (one per vendor).
        var latestRateIds = await db.RateCardItems
            .Where(i =>
                i.RateCard.DeletedAt == null &&
                i.RateCard.Status == RateCardStatus.Submitted &&
                i.ItemShadowId == itemShadowId)
            .GroupBy(i => i.RateCard.VendorShadowId)
            .Select(g => g.OrderByDescending(i => i.RateCard.SubmittedAt).First().Id)
            .ToListAsync(ct);

        return await db.RateCardItems
            .Include(i => i.RateCard)
                .ThenInclude(r => r.Vendor)
            .Include(i => i.Uom)
            .Where(i => latestRateIds.Contains(i.Id))
            .OrderBy(i => i.RateCard.Vendor.CardName)
            .ToListAsync(ct);
    }

    public Task<RateCard> CreateAsync(RateCard rateCard, CancellationToken ct = default)
    {
        db.RateCards.Add(rateCard);
        return Task.FromResult(rateCard);
    }

    public Task UpdateAsync(RateCard rateCard, CancellationToken ct = default)
    {
        db.RateCards.Update(rateCard);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(long id, CancellationToken ct = default)
        => db.RateCards.AnyAsync(r => r.Id == id && r.DeletedAt == null, ct);

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await db.RateCards
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.DeletedAt, DateTime.UtcNow), ct);
}
