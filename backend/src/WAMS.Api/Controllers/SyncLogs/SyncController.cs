namespace WAMS.Api.Controllers.SyncLogs;

using Microsoft.AspNetCore.Authorization;
using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.SyncLogs;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.SyncLogs;
using WAMS.Domain.Exceptions;
using WAMS.Infrastructure.ExternalSync.Common;

[ApiController]
[Route("api/v1/sync")]
[Authorize]
public class SyncController(
    IEnumerable<IExternalSyncService> syncServices,
    ISyncLogService syncLogService,
    ICacheInvalidationService cacheInvalidationService,
    ILogger<SyncController> logger) : BaseController
{
    /// <summary>
    /// Manually trigger full sync for all master data sources.
    /// Requires: system.sync.execute permission.
    /// </summary>
    [HttpPost("trigger")]
    [RequirePermission(Permissions.System.SyncExecute)]
    public async Task<IActionResult> TriggerSync(CancellationToken ct)
    {
        logger.LogInformation(
            "[SyncController] Manual sync triggered by userId={UserId}", 
            GetUserId()
        );

        var results = new List<object>();

        foreach (var service in syncServices)
        {
            var result = await service.SyncAllAsync(ct);

            results.Add(new
            {
                service = result.ServiceName,
                success = result.Success,
                added = result.Added,
                updated = result.Updated,
                deactivated = result.Deactivated,
                skipped = result.Skipped,
                error = result.ErrorMessage,
            });

            if (result.Success && result.ServiceName == "WarehouseSync")
                await cacheInvalidationService.InvalidateWarehouseShadowsAsync(ct);

            if (result.Success && result.ServiceName == "PpnSync")
            {
                await cacheInvalidationService.InvalidateTaxTypesAsync(ct);
                await cacheInvalidationService.InvalidateRateCardsAsync(ct);
            }
        }

        return Ok(OkResponse(
            results,
            SuccessMessages.Sync.Completed
        ));
    }

    /// <summary>
    /// Trigger a specific sync service by name.
    /// e.g. POST /api/v1/sync/trigger/WarehouseSync
    /// </summary>
    [HttpPost("trigger/{serviceName}")]
    [RequirePermission(Permissions.System.SyncExecute)]
    public async Task<IActionResult> TriggerSingleSync(string serviceName, CancellationToken ct)
    {
        var service = syncServices.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException(ErrorMessages.Sync.ServiceNotFound(serviceName));

        logger.LogInformation(
            "[SyncController] Manual sync for {Service} triggered by userId={UserId}",
            serviceName,
            GetUserId()
        );

        var result = await service.SyncAllAsync(ct);

        if (result.Success && result.ServiceName == "WarehouseSync")
            await cacheInvalidationService.InvalidateWarehouseShadowsAsync(ct);

        if (result.Success && result.ServiceName == "PpnSync")
        {
            await cacheInvalidationService.InvalidateTaxTypesAsync(ct);
            await cacheInvalidationService.InvalidateRateCardsAsync(ct);
        }

        return Ok(OkResponse(
            result,
            SuccessMessages.Sync.ServiceCompleted(serviceName)
        ));
    }

    /// <summary>
    /// Paginated history of all sync runs.
    /// Requires: system.sync.read permission.
    /// </summary>
    [HttpGet("logs")]
    [RequirePermission(Permissions.System.SyncRead)]
    public async Task<IActionResult> GetLogs([FromQuery] SyncLogQuery query, CancellationToken ct)
    {
        var result = await syncLogService.GetPagedAsync(query, ct);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta
        ));
    }

    /// <summary>
    /// Latest sync run per (ServiceName, CompanyCode) pair - for dashboard health cards.
    /// Requires: system.sync.read permission.
    /// </summary>
    [HttpGet("logs/latest")]
    [RequirePermission(Permissions.System.SyncRead)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var items = await syncLogService.GetLatestPerServiceAsync(ct);

        return Ok(OkResponse(
            items,
            SuccessMessages.Sync.LatestPerService
        ));
    }
}