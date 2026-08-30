namespace WAMS.Application.Interfaces.SyncLogs;

using WAMS.Application.DTOs.SyncLogs;
using WAMS.Domain.Entities.SyncLogs;

public interface ISyncLogRepository
{
    Task<(List<SyncLog> Items, int TotalCount)> GetPagedAsync(SyncLogQuery query, CancellationToken ct);
    Task<List<SyncLog>> GetLatestPerServiceAsync(CancellationToken ct);
}
