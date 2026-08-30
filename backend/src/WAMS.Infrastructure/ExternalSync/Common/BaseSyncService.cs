namespace WAMS.Infrastructure.ExternalSync.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Infrastructure.Data;

public abstract class BaseSyncService<TDto, TEntity>(
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger logger) : IExternalSyncService
    where TEntity : class
{
    public abstract string ServiceName { get; }

    // Fetch data from ERP. Return null if ERP is unavailable.
    protected abstract Task<List<TDto>?> FetchAsync(string companyCode, CancellationToken ct);

    // Validate required fields on a single DTO. Throw SyncSchemaException on failure.
    // Include the serialized DTO in the exception message for debug.
    protected abstract void ValidateSchema(TDto dto);

    // Load all shadow rows for this company (active + inactive) from the correct DbSet.
    protected abstract Task<List<TEntity>> GetExistingAsync(
        AppDbContext db,
        long companyId,
        CancellationToken ct);

    // Apply upsert + soft-deactivate to the EF change tracker. DO NOT call SaveChanges.
    protected abstract (int added, int updated, int deactivated) ApplyDiff(
        AppDbContext db,
        long companyId,
        List<TEntity> existing,
        List<TDto> incoming,
        DateTime now);

    public async Task<SyncResult> SyncAllAsync(CancellationToken ct = default)
    {
        List<(long Id, string Code)> companies;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var rows = await db.Companies
                .Where(c => c.IsActive && c.Code != null)
                .Select(c => new { c.Id, c.Code })
                .ToListAsync(ct);
            companies = rows.Select(c => (c.Id, Code: c.Code!)).ToList();
        }

        if (companies.Count == 0)
        {
            logger.LogWarning("[{Service}] No active companies found, skipping sync", ServiceName);
            return new SyncResult(ServiceName, 0, 0, 0, 0, true);
        }

        var tasks = companies.Select(c => SyncCompanyAsync(c.Id, c.Code, ct));
        var results = await Task.WhenAll(tasks);

        return new SyncResult(
            ServiceName,
            results.Sum(r => r.Added),
            results.Sum(r => r.Updated),
            results.Sum(r => r.Deactivated),
            results.Sum(r => r.Skipped),
            results.All(r => r.Success)
        );
    }

    private async Task<SyncResult> SyncCompanyAsync(
        long companyId,
        string companyCode,
        CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;

        var data = await FetchAsync(companyCode, ct);

        // Null / empty guard
        if (data is null || data.Count == 0)
        {
            logger.LogWarning(
                "[{Service}] ERP returned null or empty for company={Code}, skipping",
                ServiceName,
                companyCode);

            await WriteSyncLogAsync(
                companyCode,
                startedAt,
                SyncOutcome.ErpUnavailable,
                0,
                0,
                0,
                "ERP returned null or empty response",
                ct
            );

            return new SyncResult(ServiceName, 0, 0, 0, 1, true);
        }

        // Schema validation
        try
        {
            foreach (var dto in data)
                ValidateSchema(dto);
        }
        catch (SyncSchemaException ex)
        {
            logger.LogError(
                ex,
                "[{Service}] Schema validation failed for company={Code}",
                ServiceName,
                companyCode);

            await WriteSyncLogAsync(
                companyCode,
                startedAt,
                SyncOutcome.SchemaError,
                0,
                0,
                0,
                ex.Message,
                ct
            );

            return new SyncResult(ServiceName, 0, 0, 0, 1, false, ex.Message);
        }

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Load existing
            var existing = await GetExistingAsync(db, companyId, ct);

            // Apply diff (EF change tracker only, no SaveChanges yet)
            var (added, updated, deactivated) = ApplyDiff(
                db,
                companyId,
                existing,
                data,
                DateTime.UtcNow
            );

            // Commit
            await db.SaveChangesAsync(ct);

            // Write success log use CancellationToken.None: data is already committed
            // and the log must be written regardless of host shutdown
            await WriteSyncLogAsync(
                companyCode,
                startedAt,
                SyncOutcome.Success,
                added,
                updated,
                deactivated,
                null,
                CancellationToken.None
            );

            logger.LogInformation(
                "[{Service}] company={Code} added={A} updated={U} deactivated={D}",
                ServiceName,
                companyCode,
                added,
                updated,
                deactivated);

            return new SyncResult(ServiceName, added, updated, deactivated, 0, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "[{Service}] Unhandled exception for company={Code}",
                ServiceName,
                companyCode);

            await WriteSyncLogAsync(
                companyCode,
                startedAt,
                SyncOutcome.Exception,
                0,
                0,
                0,
                ex.Message,
                ct
            );

            return new SyncResult(ServiceName, 0, 0, 0, 1, false, ex.Message);
        }
    }

    private async Task WriteSyncLogAsync(
        string companyCode,
        DateTime startedAt,
        SyncOutcome outcome,
        int added,
        int updated,
        int deactivated,
        string? abortReason,
        CancellationToken ct)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.SyncLogs.Add(new SyncLog
            {
                ServiceName = ServiceName,
                CompanyCode = companyCode,
                StartedAt = startedAt,
                FinishedAt = DateTime.UtcNow,
                Outcome = outcome,
                Added = added,
                Updated = updated,
                Deactivated = deactivated,
                AbortReason = abortReason,
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // SyncLog write failure must not affect the sync result
            logger.LogError(
                ex,
                "[{Service}] Failed to write SyncLog for company={Code}",
                ServiceName,
                companyCode);
        }
    }
}
