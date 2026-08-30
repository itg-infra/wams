namespace WAMS.Application.Services.SyncLogs;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.SyncLogs;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Domain.Entities.SyncLogs;

public class SyncLogService(ISyncLogRepository repo) : ISyncLogService
{
    public async Task<PaginatedResponse<SyncLogResponse>> GetPagedAsync(SyncLogQuery query, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(query, ct);
        var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)query.Limit);

        return new PaginatedResponse<SyncLogResponse>(
            true,
            [.. items.Select(ToResponse)],
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    public async Task<List<SyncLogLatestResponse>> GetLatestPerServiceAsync(CancellationToken ct)
    {
        var items = await repo.GetLatestPerServiceAsync(ct);

        return [.. items.Select(ToLatestResponse)];
    }

    private static SyncLogResponse ToResponse(SyncLog l) => new(
        l.Id,
        l.ServiceName,
        l.CompanyCode,
        l.StartedAt,
        l.FinishedAt,
        l.Outcome.ToString(),
        l.Added,
        l.Updated,
        l.Deactivated,
        l.AbortReason,
        l.FinishedAt.HasValue ? (l.FinishedAt.Value - l.StartedAt).TotalMilliseconds : null
    );

    private static SyncLogLatestResponse ToLatestResponse(SyncLog l) => new(
        l.ServiceName,
        l.CompanyCode,
        l.StartedAt,
        l.FinishedAt,
        l.Outcome.ToString(),
        l.Added,
        l.Updated,
        l.Deactivated,
        l.AbortReason,
        l.FinishedAt.HasValue ? (l.FinishedAt.Value - l.StartedAt).TotalMilliseconds : null
    );
}
