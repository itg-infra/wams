namespace WAMS.Application.Interfaces.AuditLogs;

public interface IAuditLogWriter
{
    Task LogAsync(
        string action,
        string tableName,
        long? recordId = null,
        long? userId = null,
        string? userEmail = null,
        string? userFullname = null,
        long? companyId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? oldValues = null,
        string? newValues = null,
        CancellationToken ct = default);
}
