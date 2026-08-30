namespace WAMS.Api.Controllers.Warehouses;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Warehouses;

[ApiController]
[Route("api/v1/warehouses")]
[Authorize]
public class WarehousesController(
    IWarehouseShadowService warehouseService, 
    IExportService exportService, 
    IOptions<ExportOptions> exportOptions
) : BaseController
{
    /// <summary>Exports warehouses matching the query to a file stream.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.User.WarehouseExport)]
    public async Task<IActionResult> Export(
        [FromQuery] WarehouseQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var limit = exportOptions.Value.MaxRows;
        var stream = warehouseService.StreamAllAsync(GetUserId(), query, limit, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            WarehouseExportColumns.Columns,
            $"warehouses-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Warehouses",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>
    /// List warehouses (filtered by user's warehouse access, optional province filter)
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.User.WarehouseRead)]
    [ProducesResponseType(typeof(PaginatedResponse<WarehouseResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] WarehouseQuery query)
    {
        var userId = GetUserId();
        var result = await warehouseService.GetAllAsync(userId, query);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.Warehouse.ListRetrieved
        ));
    }

    /// <summary>
    /// Get distinct location values for the company (scoped to user's accessible warehouses)
    /// </summary>
    [HttpGet("locations")]
    [RequirePermission(Permissions.User.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<LocationListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLocations()
    {
        var userId = GetUserId();
        var locations = await warehouseService.GetDistinctLocationsAsync(userId);

        return Ok(OkResponse(
            new LocationListResponse(locations),
            SuccessMessages.Warehouse.LocationsRetrieved
        ));
    }

    /// <summary>
    /// List active warehouses with no province mapping (global access only)
    /// </summary>
    [HttpGet("unmapped")]
    [RequirePermission(Permissions.User.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<List<WarehouseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUnmapped()
    {
        var userId = GetUserId();
        var result = await warehouseService.GetUnmappedAsync(userId);

        return Ok(OkResponse(
            result,
            SuccessMessages.Warehouse.UnmappedRetrieved
        ));
    }

    /// <summary>
    /// Get warehouse by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.User.WarehouseRead)]
    [ProducesResponseType(typeof(ApiResponse<WarehouseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(long id)
    {
        var userId = GetUserId();
        var result = await warehouseService.GetByIdAsync(id, userId);

        return Ok(OkResponse(
            result,
            SuccessMessages.Warehouse.Retrieved
        ));
    }
}
