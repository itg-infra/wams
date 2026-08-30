namespace WAMS.Application.Interfaces.AuditLogs;

using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.Common;
using WAMS.Domain.Entities.AuditLogs;

public interface IAuditLogRepository
{
    Task<(List<AuditLog> Items, int TotalCount)> GetAllAsync(AuditLogQuery query, CancellationToken ct = default);
    IAsyncEnumerable<AuditLogResponse> StreamAllAsync(AuditLogQuery query, int limit, CancellationToken ct = default);
    Task<AuditLog?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<(List<AuditLog> Items, int TotalCount)> GetRecordHistoryAsync(string tableName, long recordId, DataTableQuery query, CancellationToken ct = default);
}
