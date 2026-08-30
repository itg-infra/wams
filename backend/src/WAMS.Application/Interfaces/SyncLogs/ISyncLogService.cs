namespace WAMS.Application.Interfaces.SyncLogs;

using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.SyncLogs;

public interface ISyncLogService
{
    Task<PaginatedResponse<SyncLogResponse>> GetPagedAsync(SyncLogQuery query, CancellationToken ct);
    Task<List<SyncLogLatestResponse>> GetLatestPerServiceAsync(CancellationToken ct);
}
