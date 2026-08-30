namespace WAMS.Api.Controllers.Spk;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Spk;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Spk;
using WAMS.Domain.Entities.Spk;

[ApiController]
[Route("api/v1/spk")]
[Authorize]
public class SpkController(
    ISpkService spkService,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions
) : BaseController
{
    /// <summary>Gets a paginated list of SPK records.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.PlanRead)]
    [ProducesResponseType(typeof(PaginatedResponse<SpkShadowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] SpkQuery query, CancellationToken ct)
    {
        var (items, total) = await spkService.GetAllAsync(query, GetUserId(), ct);
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
            SuccessMessages.Spk.ListRetrieved
        ));
    }

    /// <summary>Gets a SPK record by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.PlanRead)]
    [ProducesResponseType(typeof(ApiResponse<SpkShadowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var spk = await spkService.GetByIdAsync(id, GetUserId(), ct);

        return Ok(OkResponse(
            MapResponse(spk),
            SuccessMessages.Spk.Retrieved
        ));
    }

    /// <summary>Exports SPK records as a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.PlanExport)]
    public async Task<IActionResult> Export(
        [FromQuery] SpkQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = spkService.StreamAllAsync(query, GetUserId(), exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            SpkExportColumns.Columns,
            $"spk-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "SPK",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    private static SpkShadowResponse MapResponse(SpkShadow s) => new(
        s.Id,
        s.Type,
        s.DocNo,
        s.BaseDoc,
        s.BaseDocNo,
        s.CardCode,
        s.CardName,
        s.ItemCode,
        s.ItemName,
        s.Quantity,
        s.DeliveryQty,
        s.UoM,
        s.PackType,
        s.WhsCode,
        s.WhsName,
        s.DocStatus,
        s.BlNo
    );
}
