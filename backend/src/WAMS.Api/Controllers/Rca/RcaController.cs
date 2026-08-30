namespace WAMS.Api.Controllers.Rca;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Application.DTOs.Rca;
using WAMS.Application.Interfaces.Rca;
using WAMS.Domain.Constants;

[ApiController]
[Route("api/v1/rca")]
[Authorize]
public class RcaController(IRcaService rcaService, IRcaPdfRenderer renderer) : BaseController
{
    /// <summary>Exports the RCA report as a PDF for the given warehouse and date range.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Rca.ReportExport)]
    public async Task<IActionResult> Export(
        [FromQuery] string warehouseCode,
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        CancellationToken ct)
    {
        if (dateFrom > dateTo)
            return BadRequest(ErrorResponse(ErrorMessages.Rca.InvalidDateRange));

        var query = new RcaQuery(warehouseCode, dateFrom, dateTo);
        var document = await rcaService.GetDocumentAsync(query, GetUserId(), ct);
        var bytes = renderer.Render(document);
        var fileName = $"RCA-{warehouseCode}-{dateFrom:yyyyMMdd}-{dateTo:yyyyMMdd}.pdf";

        return File(
            bytes,
            "application/pdf",
            fileName
        );
    }
}
