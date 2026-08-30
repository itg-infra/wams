namespace WAMS.Infrastructure.Repositories.SyncLogs;

using Microsoft.EntityFrameworkCore;
using WAMS.Application.DTOs.SyncLogs;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Infrastructure.Data;

public class SyncLogRepository(AppDbContext db) : ISyncLogRepository
{
    public async Task<(List<SyncLog> Items, int TotalCount)> GetPagedAsync(
        SyncLogQuery query, CancellationToken ct)
    {
        var q = db.SyncLogs.AsNoTracking();

        if (query.ServiceName is not null)
            q = q.Where(l => l.ServiceName == query.ServiceName);
        if (query.CompanyCode is not null)
            q = q.Where(l => l.CompanyCode == query.CompanyCode);
        if (query.Outcome is not null)
            q = q.Where(l => l.Outcome == query.Outcome);
        if (query.DateFrom is not null)
            q = q.Where(l => l.StartedAt >= query.DateFrom);
        if (query.DateTo is not null)
            q = q.Where(l => l.StartedAt <= query.DateTo);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(l => l.StartedAt)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<List<SyncLog>> GetLatestPerServiceAsync(CancellationToken ct)
    {
        var maxByGroup = db.SyncLogs
            .AsNoTracking()
            .GroupBy(l => new { l.ServiceName, l.CompanyCode })
            .Select(g => new
            {
                g.Key.ServiceName,
                g.Key.CompanyCode,
                Latest = g.Max(l => l.StartedAt)
            });

        return await db.SyncLogs
            .AsNoTracking()
            .Where(l => maxByGroup.Any(m =>
                m.ServiceName == l.ServiceName &&
                m.CompanyCode == l.CompanyCode &&
                m.Latest == l.StartedAt))
            .OrderBy(l => l.ServiceName)
            .ThenBy(l => l.CompanyCode)
            .ToListAsync(ct);
    }
}
