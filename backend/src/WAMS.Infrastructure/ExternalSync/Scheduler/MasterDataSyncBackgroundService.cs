namespace WAMS.Infrastructure.ExternalSync.Scheduler;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.SyncLogs;
using WAMS.Infrastructure.Caching.Common;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.ExternalSync.Common;

public class MasterDataSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    IWamsMetrics metrics,
    HybridCache cache,
    ILogger<MasterDataSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Sync immediately on startup
        if (config.GetValue("ErpApi:SyncRunOnStartup", true)) await RunAllSyncsAsync(stoppingToken);

        logger.LogInformation(
            "[MasterDataSync] Scheduler started. Peak interval={Peak}min Off-peak={OffPeak}min Window={Start:D2}:00-{End:D2}:00 ({TZ}) WeekdaysOnly={WeekdaysOnly}",
            config.GetValue("ErpApi:SyncIntervalMinutesPeak", 5),
            config.GetValue("ErpApi:SyncIntervalMinutes", 60),
            config.GetValue("ErpApi:SyncPeakWindowStartHour", 8),
            config.GetValue("ErpApi:SyncPeakWindowEndHour", 17),
            ResolveTimeZone(config.GetValue("ErpApi:SyncPeakTimeZoneId", "Asia/Jakarta")).Id,
            config.GetValue("ErpApi:SyncPeakWeekdaysOnly", true));

        // PeriodicTimer.Period is fixed at construction, and the interval here changes by
        // time of day, so a plain delay loop (recomputed each iteration) is simpler than
        // fighting the timer's period after the fact.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(GetIntervalMinutes()), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunAllSyncsAsync(stoppingToken);
        }
    }

    // public for direct unit testing - it's a pure function, no need to stand up the whole service.
    public static bool IsOfficeHours(DateTime utcNow, TimeZoneInfo tz, int startHour, int endHour, bool weekdaysOnly)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        if (weekdaysOnly && local.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return false;
        return local.Hour >= startHour && local.Hour < endHour;
    }

    private int GetIntervalMinutes()
    {
        var tz = ResolveTimeZone(config.GetValue("ErpApi:SyncPeakTimeZoneId", "Asia/Jakarta"));
        var isOfficeHours = IsOfficeHours(
            DateTime.UtcNow,
            tz,
            config.GetValue("ErpApi:SyncPeakWindowStartHour", 8),
            config.GetValue("ErpApi:SyncPeakWindowEndHour", 17),
            config.GetValue("ErpApi:SyncPeakWeekdaysOnly", true));

        return isOfficeHours
            ? config.GetValue("ErpApi:SyncIntervalMinutesPeak", 5)
            : config.GetValue("ErpApi:SyncIntervalMinutes", 60);
    }

    private TimeZoneInfo ResolveTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException)
        {
            logger.LogWarning(
                "[MasterDataSync] TimeZone '{Id}' not found - falling back to UTC. Check ErpApi:SyncPeakTimeZoneId.", id);
            return TimeZoneInfo.Utc;
        }
    }

    private async Task RunAllSyncsAsync(CancellationToken ct)
    {
        logger.LogInformation("[MasterDataSync] Sync run started at {Time:u}", DateTime.UtcNow);

        await using var scope = scopeFactory.CreateAsyncScope();
        var syncServices = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IExternalSyncService>>()
            .ToList();

        // Stale detection: warn if any service has not succeeded within 2× the interval
        await CheckForStaleServicesAsync(syncServices, ct);

        foreach (var service in syncServices)
        {
            if (ct.IsCancellationRequested) break;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await service.SyncAllAsync(ct);
                sw.Stop();

                metrics.RecordErpSyncRun(result.ServiceName, result.Success);
                metrics.RecordErpSyncDuration(result.ServiceName, sw.Elapsed.TotalMilliseconds);

                if (result.Success)
                {
                    metrics.RecordErpSyncItemsUpserted(result.ServiceName, result.Added, result.Updated);

                    logger.LogInformation(
                        "[MasterDataSync] {Service} - added={A} updated={U} deactivated={D} skipped={S}",
                        result.ServiceName,
                        result.Added,
                        result.Updated,
                        result.Deactivated,
                        result.Skipped);

                    if (result.ServiceName == "WarehouseSync")
                        await cache.RemoveByTagAsync(CacheTags.WarehouseShadows, ct);

                    if (result.ServiceName == "PpnSync")
                    {
                        await cache.RemoveByTagAsync(CacheTags.TaxTypes, ct);
                        await cache.RemoveByTagAsync(CacheTags.RateCards, ct);
                    }
                }
                else
                {
                    metrics.RecordErpSyncFailure(result.ServiceName);
                    logger.LogError(
                        "[MasterDataSync] {Service} failed - {Error}",
                        result.ServiceName,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                sw.Stop();
                metrics.RecordErpSyncRun(service.ServiceName, false);
                metrics.RecordErpSyncFailure(service.ServiceName);
                metrics.RecordErpSyncDuration(service.ServiceName, sw.Elapsed.TotalMilliseconds);
                logger.LogError(ex, "[MasterDataSync] Unhandled exception in {Service}", service.ServiceName);
            }
        }
    }

    private async Task CheckForStaleServicesAsync(
        IEnumerable<IExternalSyncService> services,
        CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2 * GetIntervalMinutes());

        try
        {
            await using var staleCheckScope = scopeFactory.CreateAsyncScope();
            var dbFactory = staleCheckScope.ServiceProvider
                .GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var latestSuccess = await db.SyncLogs
                .Where(l => l.Outcome == SyncOutcome.Success && l.StartedAt >= cutoff)
                .GroupBy(l => l.ServiceName)
                .Select(g => new { ServiceName = g.Key, Latest = g.Max(l => l.FinishedAt) })
                .ToDictionaryAsync(x => x.ServiceName, x => x.Latest, ct);

            foreach (var service in services)
            {
                if (!latestSuccess.TryGetValue(service.ServiceName, out var lastSuccess))
                {
                    logger.LogInformation(
                        "[MasterDataSync] {Service} has no successful sync within the staleness window (may be first run)",
                        service.ServiceName);
                }
                else if (lastSuccess < cutoff)
                {
                    logger.LogWarning(
                        "[MasterDataSync] STALE: {Service} last succeeded at {Time:u}, threshold={Cutoff:u}",
                        service.ServiceName,
                        lastSuccess,
                        cutoff);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[MasterDataSync] Failed to run stale detection check");
        }
    }
}
