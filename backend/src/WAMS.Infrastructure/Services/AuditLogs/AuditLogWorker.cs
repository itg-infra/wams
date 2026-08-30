namespace WAMS.Infrastructure.Services.AuditLogs;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WAMS.Infrastructure.Data;

public sealed class AuditLogWorker(
    IAuditLogQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<AuditLogWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in queue.ReadBatchesAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.AuditLogs.AddRange(batch);
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist {Count} audit log(s)", batch.Count);
            }
        }
    }
}
