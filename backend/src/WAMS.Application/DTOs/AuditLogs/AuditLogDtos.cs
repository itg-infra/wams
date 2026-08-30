namespace WAMS.Application.DTOs.AuditLogs;

using System.Text.Json;
using WAMS.Application.Common;

public record AuditLogQuery : DataTableQuery
{
    public string? TableName { get; init; }
    public long? RecordId { get; init; }
    public long? UserId { get; init; }
    public string? Action { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public long? CompanyId { get; init; }
}

public record RecordHistoryResponse(
    long Id,
    string Action,
    long? UserId,
    string? UserEmail,
    string? UserFullname,
    JsonElement? OldValues,
    JsonElement? NewValues,
    DateTime CreatedAt
);

public record AuditLogResponse(
    long Id,
    string Action,
    string TableName,
    long? RecordId,
    string? RecordKey,
    long? UserId,
    string? UserEmail,
    string? UserFullname,
    long? CompanyId,
    JsonElement? OldValues,
    JsonElement? NewValues,
    string? RequestId,
    string? RequestPath,
    string? HttpMethod,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt
);
