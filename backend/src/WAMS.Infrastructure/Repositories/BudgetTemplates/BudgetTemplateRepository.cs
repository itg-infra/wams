namespace WAMS.Infrastructure.Repositories.BudgetTemplates;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;

public class BudgetTemplateRepository(AppDbContext db) : IBudgetTemplateRepository
{
    public async Task<(List<BudgetTemplate> Items, int TotalCount)> GetAllAsync(
        BudgetTemplateStatus? status,
        BudgetTemplateQuery q,
        List<long>? provinceFilter = null,
        CancellationToken ct = default
    )
    {
        var query = db.BudgetTemplates
            .Where(b => b.DeletedAt == null)
            .Include(b => b.Province)
            .AsQueryable();

        if (status is not null)
            query = query.Where(b => b.Status == status);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(b =>
                EF.Functions.ILike(b.Code, pattern, "\\") ||
                (b.Province != null && EF.Functions.ILike(b.Province.Name, pattern, "\\")));
        }

        if (q.DateFrom.HasValue)
        {
            var from = q.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(b => b.CreatedAt >= from);
        }

        if (q.DateTo.HasValue)
        {
            var to = q.DateTo.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);
            query = query.Where(b => b.CreatedAt < to);
        }

        if (provinceFilter is not null)
            query = query.Where(t => t.ProvinceId != null && provinceFilter.Contains(t.ProvinceId.Value));

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("code", true) => query.OrderByDescending(b => b.Code),
            ("code", false) => query.OrderBy(b => b.Code),
            ("status", true) => query.OrderByDescending(b => b.Status),
            ("status", false) => query.OrderBy(b => b.Status),
            ("createdat", true) => query.OrderByDescending(b => b.CreatedAt),
            ("createdat", false) => query.OrderBy(b => b.CreatedAt),
            ("submittedat", true) => query.OrderByDescending(b => b.SubmittedAt),
            ("submittedat", false) => query.OrderBy(b => b.SubmittedAt),
            _ => query.OrderByDescending(b => b.CreatedAt),
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .AsNoTracking()
            .Skip((q.Page - 1) * q.Limit)
            .Take(q.Limit)
            .ToListAsync(ct);
        return (items, total);
    }

    public IAsyncEnumerable<BudgetTemplateSummaryResponse> StreamAllAsync(
        BudgetTemplateStatus? status,
        BudgetTemplateQuery q,
        List<long>? provinceFilter,
        int limit,
        CancellationToken ct = default
    )
    {
        var query = db.BudgetTemplates
            .Where(b => b.DeletedAt == null)
            .AsQueryable();

        if (status is not null)
            query = query.Where(b => b.Status == status);

        if (provinceFilter is not null)
            query = query.Where(t => t.ProvinceId != null && provinceFilter.Contains(t.ProvinceId.Value));

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(q.Search);
            query = query.Where(b =>
                EF.Functions.ILike(b.Code, pattern, "\\") ||
                (b.Province != null && EF.Functions.ILike(b.Province.Name, pattern, "\\")));
        }

        if (q.DateFrom.HasValue)
        {
            var from = q.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(b => b.CreatedAt >= from);
        }

        if (q.DateTo.HasValue)
        {
            var to = q.DateTo.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);
            query = query.Where(b => b.CreatedAt < to);
        }

        query = (q.SortBy?.ToLowerInvariant(), q.SortOrder?.ToLowerInvariant() == "desc") switch
        {
            ("code", true) => query.OrderByDescending(b => b.Code),
            ("code", false) => query.OrderBy(b => b.Code),
            ("status", true) => query.OrderByDescending(b => b.Status),
            ("status", false) => query.OrderBy(b => b.Status),
            ("createdat", true) => query.OrderByDescending(b => b.CreatedAt),
            ("createdat", false) => query.OrderBy(b => b.CreatedAt),
            ("submittedat", true) => query.OrderByDescending(b => b.SubmittedAt),
            ("submittedat", false) => query.OrderBy(b => b.SubmittedAt),
            _ => query.OrderByDescending(b => b.CreatedAt),
        };

        return query
            .Take(limit)
            .Select(b => new BudgetTemplateSummaryResponse(
                b.Id,
                b.Code,
                b.ProvinceId,
                b.Province != null ? b.Province.Name : null,
                b.Province != null ? b.Province.Display : null,
                b.CreatedAt,
                b.Status.ToString()))
            .AsNoTracking()
            .AsAsyncEnumerable();
    }

    public async Task<BudgetTemplate?> GetByIdWithItemsAsync(long id, CancellationToken ct = default)
        => await db.BudgetTemplates
            .Where(b => b.DeletedAt == null)
            .Include(b => b.Province)
            .Include(b => b.Items)
                .ThenInclude(i => i.Item)
            .Include(b => b.Items)
                .ThenInclude(i => i.ActivityType)
            .Include(b => b.CreatedBy)
            .Include(b => b.SubmittedBy)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

    // Lightweight load for the budget-plan create/update path: only Status, ProvinceId and Items
    // (ItemShadowId -> ActivityTypeId map) are consumed, so skip the ActivityType/user joins.
    public Task<BudgetTemplate?> GetByIdForPlanSourceAsync(long id, CancellationToken ct = default)
        => db.BudgetTemplates
            .Where(b => b.DeletedAt == null && b.Id == id)
            .Include(b => b.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

    // Tracked bare entity - no navigation includes. For write paths that only need scalar fields.
    public Task<BudgetTemplate?> GetTrackedAsync(long id, CancellationToken ct = default)
        => db.BudgetTemplates
            .Where(b => b.DeletedAt == null && b.Id == id)
            .FirstOrDefaultAsync(ct);

    public Task CreateAsync(BudgetTemplate template, CancellationToken ct = default)
    {
        db.BudgetTemplates.Add(template);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BudgetTemplate template, CancellationToken ct = default)
    {
        db.BudgetTemplates.Update(template);
        return Task.CompletedTask;
    }

    public async Task SoftDeleteAsync(long id, CancellationToken ct = default)
        => await db.BudgetTemplates
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.DeletedAt, DateTime.UtcNow), ct);
}
