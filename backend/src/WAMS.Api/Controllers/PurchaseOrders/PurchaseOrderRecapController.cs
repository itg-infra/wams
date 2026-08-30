namespace WAMS.Api.Controllers.PurchaseOrders;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rfba;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/purchase-orders/recap")]
[Authorize]
public class PurchaseOrderRecapController(
    IPurchaseOrderService poService,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions,
    IRfbaFormPdfRenderer rfbaPdfRenderer,
    IPdfMetadataResolver pdfMetadataResolver
) : BaseController
{
    /// <summary>Gets a paginated list of APDP purchase order recaps.</summary>
    [HttpGet("apdp")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetApdpList([FromQuery] DataTableQuery query, CancellationToken ct)
        => GetListAsync(isRfba: true, query, ct);

    /// <summary>Gets a paginated list of non-APDP purchase order recaps.</summary>
    [HttpGet("non-apdp")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>), StatusCodes.Status200OK)]
    public Task<IActionResult> GetNonApdpList([FromQuery] DataTableQuery query, CancellationToken ct)
        => GetListAsync(isRfba: false, query, ct);

    /// <summary>Gets the APDP purchase order recap detail by id.</summary>
    [HttpGet("apdp/{poId:long}")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(ApiResponse<RecapPurchaseOrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetApdpDetail(long poId, CancellationToken ct)
        => GetDetailAsync(isRfba: true, poId, ct);

    /// <summary>Gets the non-APDP purchase order recap detail by id.</summary>
    [HttpGet("non-apdp/{poId:long}")]
    [RequirePermission(Permissions.Budget.PoRead)]
    [ProducesResponseType(typeof(ApiResponse<RecapPurchaseOrderDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetNonApdpDetail(long poId, CancellationToken ct)
        => GetDetailAsync(isRfba: false, poId, ct);

    /// <summary>Exports the RFBA form for an APDP recap purchase order.</summary>
    [HttpGet("apdp/{poId:long}/rfba-pdf")]
    [RequirePermission(Permissions.Budget.PoExport)]
    public async Task<IActionResult> ExportApdpRfbaPdf(long poId, CancellationToken ct)
    {
        var detail = await poService.GetRecapDetailAsync(true, poId, ct);
        var document = RfbaFormMapper.FromRecapPurchaseOrder(detail);

        if (document.Pages.Count == 0)
            throw new NotFoundException(ErrorMessages.PurchaseOrder.NoRfbaItems(poId));

        var metadata = await pdfMetadataResolver.ResolveAsync("RFBA", ct);
        var bytes = rfbaPdfRenderer.Render(document, metadata);

        return File(bytes, "application/pdf", $"RFBA-{detail.Code}.pdf");
    }

    /// <summary>Exports the APDP purchase order recap list.</summary>
    [HttpGet("apdp/export")]
    [RequirePermission(Permissions.Budget.PoExport)]
    public Task<IActionResult> ExportApdp([FromQuery] DataTableQuery query, [FromQuery] ExportFormat format = ExportFormat.Xlsx, CancellationToken ct = default)
        => ExportAsync(isRfba: true, query, format, ct);

    /// <summary>Exports the non-APDP purchase order recap list.</summary>
    [HttpGet("non-apdp/export")]
    [RequirePermission(Permissions.Budget.PoExport)]
    public Task<IActionResult> ExportNonApdp([FromQuery] DataTableQuery query, [FromQuery] ExportFormat format = ExportFormat.Xlsx, CancellationToken ct = default)
        => ExportAsync(isRfba: false, query, format, ct);

    private async Task<IActionResult> GetListAsync(bool isRfba, DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await poService.GetRecapAsync(isRfba, GetUserId(), query, ct);
        var meta = new PaginationMeta(query.Page, query.Limit, total, (int)Math.Ceiling(total / (double)query.Limit));

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.PurchaseOrder.RecapListRetrieved
        ));
    }

    private async Task<IActionResult> GetDetailAsync(bool isRfba, long poId, CancellationToken ct)
    {
        var result = await poService.GetRecapDetailAsync(isRfba, poId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.PurchaseOrder.RecapRetrieved
        ));
    }

    private async Task<IActionResult> ExportAsync(bool isRfba, DataTableQuery query, ExportFormat format, CancellationToken ct)
    {
        var stream = poService.StreamRecapAsync(isRfba, GetUserId(), query, exportOptions.Value.MaxRows, ct);
        var typeSlug = isRfba ? "apdp" : "non-apdp";

        await StreamExportResponseAsync(
            stream,
            format,
            RecapPurchaseOrderExportColumns.Columns,
            $"recap-purchase-orders-{typeSlug}-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Recap Purchase Orders",
            exportService,
            ct
        );

        return new EmptyResult();
    }
}
