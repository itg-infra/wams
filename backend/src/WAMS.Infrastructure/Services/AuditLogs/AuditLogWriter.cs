namespace WAMS.Infrastructure.Services.AuditLogs;

using Microsoft.AspNetCore.Http;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Domain.Entities.AuditLogs;
using WAMS.Infrastructure.Data;

public class AuditLogWriter(AppDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditLogWriter
{
    public async Task LogAsync(
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
        CancellationToken ct = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        await db.WriteAuditLogAsync(new AuditLog
        {
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            UserId = userId,
            UserEmail = userEmail,
            UserFullname = userFullname,
            CompanyId = companyId,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress ?? TryGetIpAddress(httpContext),
            UserAgent = userAgent ?? TryGetUserAgent(httpContext),
            RequestPath = httpContext?.Request.Path.Value,
            HttpMethod = httpContext?.Request.Method,
            RequestId = httpContext?.Items["RequestId"]?.ToString(),
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    private static string? TryGetIpAddress(HttpContext? ctx)
    {
        if (ctx is null) return null;
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return ctx.Connection.RemoteIpAddress?.ToString();
    }

    private static string? TryGetUserAgent(HttpContext? ctx)
    {
        if (ctx is null) return null;
        var ua = ctx.Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }
}
