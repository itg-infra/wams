namespace WAMS.Infrastructure.Repositories.AuditLogs;

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.AuditLogs;
using WAMS.Infrastructure.Data;

public class AuditLogRepository(AppDbContext db, ITenantContext tenantContext) : IAuditLogRepository
{
    public async Task<(List<AuditLog> Items, int TotalCount)> GetAllAsync(
        AuditLogQuery query,
        CancellationToken ct = default
    )
    {
        var auditQuery = db.AuditLogs.AsQueryable();

        // Tenant isolation: regular users only see their own company's logs
        if (tenantContext.IsSet && tenantContext.CompanyId.HasValue)
        {
            auditQuery = auditQuery.Where(a => a.CompanyId == tenantContext.CompanyId.Value);
        }
        else if (query.CompanyId.HasValue)
        {
            // Super Admin: allow explicit company filter
            auditQuery = auditQuery.Where(a => a.CompanyId == query.CompanyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TableName))
            auditQuery = auditQuery.Where(a => a.TableName == query.TableName);

        if (query.RecordId.HasValue)
            auditQuery = auditQuery.Where(a => a.RecordId == query.RecordId.Value);

        if (query.UserId.HasValue)
            auditQuery = auditQuery.Where(a => a.UserId == query.UserId.Value);

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.ToUpperInvariant();
            auditQuery = auditQuery.Where(a => a.Action == action);
        }

        if (query.DateFrom.HasValue)
            auditQuery = auditQuery.Where(a => a.CreatedAt >= query.DateFrom.Value.ToUniversalTime());

        if (query.DateTo.HasValue)
            auditQuery = auditQuery.Where(a => a.CreatedAt <= query.DateTo.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(query.Search);
            auditQuery = auditQuery.Where(a =>
                EF.Functions.ILike(a.TableName, pattern, "\\") ||
                (a.RequestPath != null && EF.Functions.ILike(a.RequestPath, pattern, "\\")) ||
                (a.RequestId != null && EF.Functions.ILike(a.RequestId, pattern, "\\")));
        }

        auditQuery = (query.SortBy?.ToLowerInvariant(), query.SortOrder?.ToLowerInvariant() == "asc") switch
        {
            ("tablename", true) => auditQuery.OrderBy(a => a.TableName),
            ("tablename", false) => auditQuery.OrderByDescending(a => a.TableName),
            ("action", true) => auditQuery.OrderBy(a => a.Action),
            ("action", false) => auditQuery.OrderByDescending(a => a.Action),
            ("userid", true) => auditQuery.OrderBy(a => a.UserId),
            ("userid", false) => auditQuery.OrderByDescending(a => a.UserId),
            ("createdat", true) => auditQuery.OrderBy(a => a.CreatedAt),
            ("createdat", false) => auditQuery.OrderByDescending(a => a.CreatedAt),
            _ => auditQuery.OrderByDescending(a => a.CreatedAt),
        };

        var total = await auditQuery.CountAsync(ct);
        var items = await auditQuery
            .AsNoTracking()
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async IAsyncEnumerable<AuditLogResponse> StreamAllAsync(
        AuditLogQuery query,
        int limit,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        var auditQuery = db.AuditLogs.AsQueryable();

        if (tenantContext.IsSet && tenantContext.CompanyId.HasValue)
            auditQuery = auditQuery.Where(a => a.CompanyId == tenantContext.CompanyId.Value);
        else if (query.CompanyId.HasValue)
            auditQuery = auditQuery.Where(a => a.CompanyId == query.CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(query.TableName))
            auditQuery = auditQuery.Where(a => a.TableName == query.TableName);

        if (query.RecordId.HasValue)
            auditQuery = auditQuery.Where(a => a.RecordId == query.RecordId.Value);

        if (query.UserId.HasValue)
            auditQuery = auditQuery.Where(a => a.UserId == query.UserId.Value);

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var action = query.Action.ToUpperInvariant();
            auditQuery = auditQuery.Where(a => a.Action == action);
        }

        if (query.DateFrom.HasValue)
            auditQuery = auditQuery.Where(a => a.CreatedAt >= query.DateFrom.Value.ToUniversalTime());

        if (query.DateTo.HasValue)
            auditQuery = auditQuery.Where(a => a.CreatedAt <= query.DateTo.Value.ToUniversalTime());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePatternHelper.ToContainsPattern(query.Search);
            auditQuery = auditQuery.Where(a =>
                EF.Functions.ILike(a.TableName, pattern, "\\") ||
                (a.RequestPath != null && EF.Functions.ILike(a.RequestPath, pattern, "\\")) ||
                (a.RequestId != null && EF.Functions.ILike(a.RequestId, pattern, "\\")));
        }

        auditQuery = (query.SortBy?.ToLowerInvariant(), query.SortOrder?.ToLowerInvariant() == "asc") switch
        {
            ("tablename", true) => auditQuery.OrderBy(a => a.TableName),
            ("tablename", false) => auditQuery.OrderByDescending(a => a.TableName),
            ("action", true) => auditQuery.OrderBy(a => a.Action),
            ("action", false) => auditQuery.OrderByDescending(a => a.Action),
            ("userid", true) => auditQuery.OrderBy(a => a.UserId),
            ("userid", false) => auditQuery.OrderByDescending(a => a.UserId),
            ("createdat", true) => auditQuery.OrderBy(a => a.CreatedAt),
            ("createdat", false) => auditQuery.OrderByDescending(a => a.CreatedAt),
            _ => auditQuery.OrderByDescending(a => a.CreatedAt),
        };

        await foreach (var a in auditQuery.AsNoTracking().Take(limit).AsAsyncEnumerable().WithCancellation(ct))
        {
            yield return new AuditLogResponse(
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
                a.CreatedAt);
        }
    }

    public async Task<AuditLog?> GetByIdAsync(long id, CancellationToken ct = default)
        => await db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(List<AuditLog> Items, int TotalCount)> GetRecordHistoryAsync(
        string tableName,
        long recordId,
        DataTableQuery query,
        CancellationToken ct = default
    )
    {
        var auditQuery = db.AuditLogs
            .Where(a => a.TableName == tableName && a.RecordId == recordId);

        if (tenantContext.IsSet && tenantContext.CompanyId.HasValue)
            auditQuery = auditQuery.Where(a => a.CompanyId == tenantContext.CompanyId.Value);

        auditQuery = (query.SortBy?.ToLowerInvariant(), query.SortOrder?.ToLowerInvariant() == "asc") switch
        {
            ("action", true) => auditQuery.OrderBy(a => a.Action),
            ("action", false) => auditQuery.OrderByDescending(a => a.Action),
            ("createdat", true) => auditQuery.OrderBy(a => a.CreatedAt),
            ("createdat", false) => auditQuery.OrderByDescending(a => a.CreatedAt),
            _ => auditQuery.OrderByDescending(a => a.CreatedAt),
        };

        var total = await auditQuery.CountAsync(ct);
        var items = await auditQuery
            .AsNoTracking()
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToListAsync(ct);

        return (items, total);
    }
}
