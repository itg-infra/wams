namespace WAMS.Api.Controllers.Vendors;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Vendors;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/vendors")]
[Authorize]
public class VendorsController(
    IVendorShadowRepository vendorRepo, 
    IExportService exportService, 
    IOptions<ExportOptions> exportOptions
) : BaseController
{
    /// <summary>Exports vendors matching the query to a file stream.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.VendorExport)]
    public async Task<IActionResult> Export(
        [FromQuery] DataTableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = vendorRepo.StreamAllAsync(query, exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            VendorExportColumns.Columns,
            $"vendors-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Vendors",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets a paginated list of vendors.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.VendorRead)]
    [ProducesResponseType(typeof(PaginatedResponse<VendorSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await vendorRepo.GetAllAsync(query, ct);
        var data = items.Select(v => new VendorSummaryResponse(v.Id, v.CardCode, v.CardName)).ToList();
        var meta = new PaginationMeta(
            query.Page, 
            query.Limit, 
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            data,
            meta,
            SuccessMessages.Vendor.ListRetrieved
        ));
    }

    /// <summary>Gets a vendor by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.VendorRead)]
    [ProducesResponseType(typeof(ApiResponse<VendorSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var vendor = await vendorRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("Vendor", id);

        return Ok(OkResponse(
            new VendorSummaryResponse(vendor.Id, vendor.CardCode, vendor.CardName),
            SuccessMessages.Vendor.Retrieved
        ));
    }
}
