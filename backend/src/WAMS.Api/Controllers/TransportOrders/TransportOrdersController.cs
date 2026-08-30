namespace WAMS.Api.Controllers.TransportOrders;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.TransportOrders;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.TransportOrders;
using WAMS.Domain.Entities.TransportOrders;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/transport-orders")]
[Authorize]
public class TransportOrdersController(
    ITransportOrderShadowRepository toRepo,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions
) : BaseController
{
    /// <summary>Gets a paginated list of transport orders.</summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkOrder.Read)]
    [ProducesResponseType(typeof(PaginatedResponse<TransportOrderShadowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] TransportOrderQuery query, CancellationToken ct)
    {
        var (items, total) = await toRepo.GetAllAsync(query, ct);
        var responses = items.Select(MapResponse).ToList();
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            responses,
            meta,
            SuccessMessages.TransportOrder.ListRetrieved
        ));
    }

    /// <summary>Gets a transport order by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.Read)]
    [ProducesResponseType(typeof(ApiResponse<TransportOrderShadowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var to = await toRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException(ErrorMessages.TransportOrder.NotFound(id));

        return Ok(OkResponse(
            MapResponse(to),
            SuccessMessages.TransportOrder.Retrieved
        ));
    }

    /// <summary>Exports transport orders matching the query to a file stream.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.WorkOrder.Export)]
    public async Task<IActionResult> Export(
        [FromQuery] TransportOrderQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = toRepo.StreamAllAsync(query, exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            TransportOrderExportColumns.Columns,
            $"transport-orders-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Transport Orders",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    private static TransportOrderShadowResponse MapResponse(TransportOrderShadow t) => new(
        t.Id,
        t.DocNo,
        t.Type,
        t.CardCode,
        t.CardName,
        t.VehicleNo,
        t.VehicleType,
        t.BlNo,
        t.ItemCode,
        t.ItemName,
        t.Quantity,
        t.UoM,
        t.WhsCode,
        t.WhsName,
        t.DocStatus
    );
}
