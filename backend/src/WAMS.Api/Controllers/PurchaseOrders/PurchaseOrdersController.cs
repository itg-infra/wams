namespace WAMS.Api.Controllers.PurchaseOrders;

using WAMS.Api.Controllers.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.PurchaseOrders;

[ApiController]
[Route("api/v1/purchase-orders")]
[Authorize]
public class PurchaseOrdersController(
    IPurchaseOrderService poService,
    IValidator<CreatePurchaseOrderRequest> createValidator,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions,
    IAuditLogService auditLogService,
    IPurchaseOrderPdfRenderer pdfRenderer,
    IPdfMetadataResolver pdfMetadataResolver
) : BaseController
{
    private const string TableName = "purchase_orders";

    /// <summary>Gets a paginated list of approved budget plans available for purchase orders.</summary>
    [HttpGet("approved-budget-plans")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovedBudgetPlans([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await poService.GetApprovedBudgetPlansAsync(GetUserId(), query, ct);
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

    /// <summary>Gets a paginated list of purchase orders.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseOrderSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PurchaseOrderQuery query, CancellationToken ct)
    {
        var (items, total) = await poService.GetAllAsync(query, ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.PurchaseOrder.ListRetrieved
        ));
    }

    /// <summary>Gets a purchase order by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await poService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.PurchaseOrder.Retrieved
        ));
    }

    /// <summary>Exports a single purchase order as a printable PDF form.</summary>
    /// <remarks>
    /// Reproduces the ERP PO form. Drafts (not yet sent to SAP) print too, but
    /// use the WAMS code instead of a SAP document number and get a DRAFT
    /// watermark plus a DRAFT- filename prefix.
    /// </remarks>
    [HttpGet("{id:long}/pdf")]
    [RequirePermission(Permissions.Budget.PoExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportPdf(long id, CancellationToken ct)
    {
        var po = await poService.GetByIdAsync(id, ct);
        var metadata = await pdfMetadataResolver.ResolveAsync("Purchase Order", ct);
        var bytes = pdfRenderer.Render(po, metadata);

        // Both Code and SapPoNumber already carry their own prefix - do not add another.
        var fileName = po.SapPoNumber ?? $"DRAFT-{po.Code}";
        return File(bytes, "application/pdf", $"{fileName}.pdf");
    }

    /// <summary>Gets paginated items available for a purchase order from a seed budget plan.</summary>
    /// <remarks>
    /// <c>BudgetPlanId</c> optionally seeds validation and ordering; results may also
    /// include same-vendor items from other approved, active plans in accessible warehouses.
    /// Each row includes its source plan and warehouse metadata, including
    /// <c>isSeedBudgetPlan</c>.
    ///
    /// <c>takenByCode</c> is the PO currently holding the item; null means it is selectable.
    /// <c>isGenerated</c> only indicates that the item is on a Generated PO and does not mean
    /// the item is free. With <c>includeGenerated=false</c>, taken items are excluded.
    /// </remarks>
    /// <param name="query">Seed plan context, vendor filter, availability flags, search, and pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("available-items")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(PaginatedResponse<AvailablePoItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableItems(
        [FromQuery] AvailablePoItemQuery query,
        CancellationToken ct)
    {
        var (items, total) = await poService.GetAvailableItemsAsync(GetUserId(), query, ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit));

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.General.AvailableItemsRetrieved));
    }

    /// <summary>Gets paginated available items while editing the specified draft purchase order.</summary>
    [HttpGet("{purchaseOrderId:long}/available-items")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(PaginatedResponse<AvailablePoItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableItemsForEdit(
        long purchaseOrderId,
        [FromQuery] EditAvailablePoItemQuery query,
        CancellationToken ct)
    {
        var (items, total) = await poService.GetAvailableItemsForEditAsync(
            GetUserId(), purchaseOrderId, query, ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit));

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.General.AvailableItemsRetrieved));
    }

    /// <summary>Creates a new purchase order.</summary>
    [HttpPost]
    [RequirePermission(Permissions.Budget.PoCreate)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new WAMS.Domain.Exceptions.ValidationException(errors);
        }

        var userId = GetUserId();
        var result = await poService.CreateAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.PurchaseOrder.Created
        ));
    }

    /// <summary>Creates a new purchase order and immediately generates it in SAP.</summary>
    [HttpPost("generate")]
    [RequirePermission(Permissions.Budget.PoGenerate)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAndGenerate([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new WAMS.Domain.Exceptions.ValidationException(errors);
        }

        if (request.Items.Count == 0)
        {
            throw new WAMS.Domain.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    [nameof(request.Items)] = [ErrorMessages.Validation.Common.AtLeastOneLineItemRequired],
                });
        }

        var userId = GetUserId();
        var result = await poService.CreateAndGenerateAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.PurchaseOrder.CreatedAndGenerated
        ));
    }

    /// <summary>Updates an existing purchase order.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Budget.PoUpdate)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken ct)
    {
        var result = await poService.UpdateAsync(id, GetUserId(), request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.PurchaseOrder.Updated
        ));
    }

    /// <summary>Deletes a purchase order by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Budget.PoDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await poService.DeleteAsync(id, ct);

        return NoContent();
    }

    /// <summary>Generates an existing purchase order in SAP.</summary>
    [HttpPost("{id:long}/generate")]
    [RequirePermission(Permissions.Budget.PoGenerate)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await poService.GenerateAsync(id, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.PurchaseOrder.Generated
        ));
    }

    /// <summary>Generates the standalone SAP APDP for the RFBA lines of a generated PO.</summary>
    [HttpPost("{id:long}/generate-apdp")]
    [RequirePermission(Permissions.Budget.PoGenerate)]
    [ProducesResponseType(typeof(ApiResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateApdp(long id, CancellationToken ct)
    {
        var result = await poService.GenerateApdpAsync(id, GetUserId(), ct);

        return Ok(OkResponse(result, SuccessMessages.PurchaseOrder.ApdpGenerated));
    }

    /// <summary>Exports purchase orders matching the given query.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.PoExport)]
    public async Task<IActionResult> Export(
        [FromQuery] PurchaseOrderQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = poService.StreamAllAsync(query, exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            PurchaseOrderExportColumns.Columns,
            $"purchase-orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Purchase Orders",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets the paginated audit history for a purchase order.</summary>
    [HttpGet("{id:long}/history")]
    [RequirePermission(Permissions.Budget.PoRead)]
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
