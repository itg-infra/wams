namespace WAMS.Application.Interfaces.AuditLogs;

using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;

public interface IAuditLogService
{
    Task<PaginatedResponse<AuditLogResponse>> GetAllAsync(AuditLogQuery query, CancellationToken ct = default);
    IAsyncEnumerable<AuditLogResponse> StreamAllAsync(AuditLogQuery query, int limit, CancellationToken ct = default);
    Task<AuditLogResponse?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PaginatedResponse<AuditLogResponse>> GetRecordHistoryAsync(string tableName, long recordId, DataTableQuery query, CancellationToken ct = default);
    Task<PaginatedResponse<RecordHistoryResponse>> GetRecordHistorySlimAsync(string tableName, long recordId, DataTableQuery query, CancellationToken ct = default);
}
