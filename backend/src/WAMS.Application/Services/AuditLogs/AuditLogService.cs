namespace WAMS.Application.Services.AuditLogs;

using System.Text.Json;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Domain.Entities.AuditLogs;

public class AuditLogService(IAuditLogRepository auditLogRepository) : IAuditLogService
{
    public async Task<PaginatedResponse<AuditLogResponse>> GetAllAsync(
        AuditLogQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await auditLogRepository.GetAllAsync(query, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.Limit);

        return new PaginatedResponse<AuditLogResponse>(
            true,
            [.. items.Select(Map)],
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    public IAsyncEnumerable<AuditLogResponse> StreamAllAsync(
        AuditLogQuery query,
        int limit,
        CancellationToken ct = default
    )
        => auditLogRepository.StreamAllAsync(query, limit, ct);

    public async Task<AuditLogResponse?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var item = await auditLogRepository.GetByIdAsync(id, ct);

        return item is null ? null : Map(item);
    }

    public async Task<PaginatedResponse<AuditLogResponse>> GetRecordHistoryAsync(
        string tableName,
        long recordId,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await auditLogRepository.GetRecordHistoryAsync(tableName, recordId, query, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.Limit);

        return new PaginatedResponse<AuditLogResponse>(
            true,
            [.. items.Select(Map)],
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    public async Task<PaginatedResponse<RecordHistoryResponse>> GetRecordHistorySlimAsync(
        string tableName,
        long recordId,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var (items, total) = await auditLogRepository.GetRecordHistoryAsync(tableName, recordId, query, ct);
        var totalPages = (int)Math.Ceiling((double)total / query.Limit);

        return new PaginatedResponse<RecordHistoryResponse>(
            true,
            [.. items.Select(MapSlim)],
            new PaginationMeta(query.Page, query.Limit, total, totalPages)
        );
    }

    private static RecordHistoryResponse MapSlim(AuditLog a) => new(
        a.Id,
        a.Action,
        a.UserId,
        a.UserEmail,
        a.UserFullname,
        a.OldValues is null ? null : JsonSerializer.Deserialize<JsonElement>(a.OldValues),
        a.NewValues is null ? null : JsonSerializer.Deserialize<JsonElement>(a.NewValues),
        a.CreatedAt
    );

    private static AuditLogResponse Map(AuditLog a) => new(
        a.Id,
        a.Action,
        a.TableName,
        a.RecordId,
        a.RecordKey,
        a.UserId,
        a.UserEmail,
        a.UserFullname,
        a.CompanyId,
        a.OldValues is null ? null : JsonSerializer.Deserialize<JsonElement>(a.OldValues),
        a.NewValues is null ? null : JsonSerializer.Deserialize<JsonElement>(a.NewValues),
        a.RequestId,
        a.RequestPath,
        a.HttpMethod,
        a.IpAddress,
        a.UserAgent,
        a.CreatedAt
    );
}
