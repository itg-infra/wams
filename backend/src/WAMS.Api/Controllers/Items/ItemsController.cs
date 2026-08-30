namespace WAMS.Api.Controllers.Items;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Items;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Items;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/items")]
[Authorize]
public class ItemsController(
    IItemShadowRepository itemRepo, 
    IExportService exportService, 
    IOptions<ExportOptions> exportOptions
) : BaseController
{
    /// <summary>Exports items matching the given query.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.ItemExport)]
    public async Task<IActionResult> Export(
        [FromQuery] DataTableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = itemRepo.StreamAllAsync(query, exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            ItemExportColumns.Columns,
            $"items-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Items",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets a paginated list of items.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.ItemRead)]
    [ProducesResponseType(typeof(PaginatedResponse<ItemSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await itemRepo.GetAllAsync(query, ct);
        var data = items.Select(i => new ItemSummaryResponse(
            i.Id, 
            i.ItemCode, 
            i.ItemName, 
            i.AcctCode, 
            i.AcctName
        )).ToList();
        var meta = new PaginationMeta(
            query.Page, 
            query.Limit, 
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            data,
            meta,
            SuccessMessages.Item.ListRetrieved
        ));
    }

    /// <summary>Gets an item by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.ItemRead)]
    [ProducesResponseType(typeof(ApiResponse<ItemSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var item = await itemRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Item", id);

        return Ok(OkResponse(
            new ItemSummaryResponse(
                item.Id, 
                item.ItemCode, 
                item.ItemName, 
                item.AcctCode, 
                item.AcctName
            ),
            SuccessMessages.Item.Retrieved
        ));
    }
}
