namespace WAMS.Api.Controllers.WorkOrders;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.WorkOrders;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.WorkOrders;

[ApiController]
[Route("api/v1/work-orders")]
[Authorize]
public class WorkOrdersController(
    IWorkOrderService woService,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions,
    IAuditLogService auditLogService
) : BaseController
{
    private const string TableName = "work_orders";

    /// <summary>Gets a paginated list of approved budget plans available for work order creation.</summary>
    [HttpGet("approved-plans")]
    [RequirePermission(Permissions.WorkOrder.Read)]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedBpForWoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovedPlans([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await woService.GetApprovedBpListAsync(
            GetUserId(), 
            query.Page, 
            query.Limit, 
            ct
        );
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.General.ApprovedBudgetPlansRetrieved
        ));
    }

    /// <summary>Gets a paginated list of work orders.</summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkOrder.Read)]
    [ProducesResponseType(typeof(PaginatedResponse<WorkOrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] WorkOrderQuery query, CancellationToken ct)
    {
        var (items, total) = await woService.GetAllAsync(query, GetUserId(), ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.WorkOrder.ListRetrieved
        ));
    }

    /// <summary>Gets a work order by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.Read)]
    [ProducesResponseType(typeof(ApiResponse<WorkOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await woService.GetByIdAsync(id, GetUserId(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkOrder.Retrieved
        ));
    }

    /// <summary>Gets the candidate PICs (persons in charge) available for a work order.</summary>
    [HttpGet("{id:long}/pic")]
    [RequirePermission(Permissions.WorkOrder.Update)]
    [ProducesResponseType(typeof(ApiResponse<List<WorkOrderPicCandidateResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPicCandidates(long id, CancellationToken ct)
    {
        var result = await woService.GetPicCandidatesAsync(id, GetUserId(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkOrder.PicCandidatesRetrieved
        ));
    }

    /// <summary>Updates an existing work order.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.Update)]
    [ProducesResponseType(typeof(ApiResponse<WorkOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateWorkOrderRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await woService.UpdateAsync(id, request, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkOrder.Updated
        ));
    }

    /// <summary>Deletes a work order by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        await woService.DeleteAsync(id, userId, ct);

        return NoContent();
    }

    /// <summary>Submits a work order for approval.</summary>
    [HttpPost("{id:long}/submit")]
    [RequirePermission(Permissions.WorkOrder.Submit)]
    [ProducesResponseType(typeof(ApiResponse<WorkOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await woService.SubmitAsync(id, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.WorkOrder.Submitted
        ));
    }

    /// <summary>Exports work orders matching the query to a file stream.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.WorkOrder.Export)]
    public async Task<IActionResult> Export(
        [FromQuery] WorkOrderQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var limit = exportOptions.Value.MaxRows;
        var stream = woService.StreamAllAsync(query, GetUserId(), limit, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            WorkOrderExportColumns.Columns,
            $"work-orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Work Orders",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets the paginated audit history for a work order.</summary>
    [HttpGet("{id:long}/history")]
    [RequirePermission(Permissions.WorkOrder.Read)]
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
