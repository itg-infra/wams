namespace WAMS.Api.Controllers.RecapWorkOrders;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.RecapWorkOrders;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.RecapWorkOrders;

[ApiController]
[Route("api/v1/recap-work-orders")]
[Authorize]
public class RecapWorkOrdersController(IRecapWorkOrderService recapService, IExportService exportService, IOptions<ExportOptions> exportOptions, IAuditLogService auditLogService) : BaseController
{
    private const string TableName = "recap_work_orders";

    /// <summary>Gets a paginated list of work order recaps.</summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkOrder.RecapRead)]
    [ProducesResponseType(typeof(PaginatedResponse<RecapWorkOrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] RecapWorkOrderQuery query, CancellationToken ct)
    {
        var (items, total) = await recapService.GetAllAsync(query, GetUserId(), ct);
        var meta = new PaginationMeta(
            query.Page, 
            query.Limit, 
            total, 
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.RecapWorkOrder.ListRetrieved
        ));
    }

    /// <summary>Gets a work order recap by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.RecapRead)]
    [ProducesResponseType(typeof(ApiResponse<RecapWorkOrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await recapService.GetByIdAsync(id, GetUserId(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RecapWorkOrder.Retrieved
        ));
    }

    /// <summary>Approves a work order recap.</summary>
    [HttpPost("{id:long}/approve")]
    [RequirePermission(Permissions.WorkOrder.RecapApprove)]
    [ProducesResponseType(typeof(ApiResponse<RecapWorkOrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var result = await recapService.ApproveAsync(id, GetUserId(), GetFullname(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RecapWorkOrder.Approved
        ));
    }

    /// <summary>Rejects a work order recap with an optional reason.</summary>
    [HttpPost("{id:long}/reject")]
    [RequirePermission(Permissions.WorkOrder.RecapReject)]
    [ProducesResponseType(typeof(ApiResponse<RecapWorkOrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectRecapRequest? request, CancellationToken ct)
    {
        var result = await recapService.RejectAsync(
            id, 
            GetUserId(), 
            GetFullname(), 
            request?.Reason, 
            ct
        );

        return Ok(OkResponse(
            result,
            SuccessMessages.RecapWorkOrder.Rejected
        ));
    }

    /// <summary>Exports work order recaps as a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.WorkOrder.RecapExport)]
    public async Task<IActionResult> Export(
        [FromQuery] RecapWorkOrderQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = recapService.StreamAllAsync(query, GetUserId(), exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            RecapWorkOrderExportColumns.Columns,
            $"recap-work-orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Recap Work Orders",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets the paginated audit history for a work order recap.</summary>
    [HttpGet("{id:long}/history")]
    [RequirePermission(Permissions.WorkOrder.RecapRead)]
    [ProducesResponseType(typeof(PaginatedResponse<RecordHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(long id, [FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var result = await auditLogService.GetRecordHistorySlimAsync(TableName, id, query, ct);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.General.HistoryRetrieved
        ));
    }
}
