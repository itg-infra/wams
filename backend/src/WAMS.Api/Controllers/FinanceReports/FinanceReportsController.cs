namespace WAMS.Api.Controllers.FinanceReports;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.FinanceReports;

[ApiController]
[Route("api/v1/finance-reports")]
[Authorize]
public class FinanceReportsController(
    IFinanceReportService financeReportService,
    IExportService exportService
) : BaseController
{
    /// <summary>Gets a paginated list of finance reports.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Report.FinanceReportRead)]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedBudgetPlanPoStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await financeReportService.GetAllAsync(query, GetUserId(), ct);
        var meta = new PaginationMeta(
            query.Page, 
            query.Limit, 
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.FinanceReport.ListRetrieved
        ));
    }

    /// <summary>Gets the finance report detail for a budget plan.</summary>
    [HttpGet("{budgetPlanId:long}")]
    [RequirePermission(Permissions.Report.FinanceReportRead)]
    [ProducesResponseType(typeof(ApiResponse<FinanceReportDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(long budgetPlanId, CancellationToken ct)
    {
        var result = await financeReportService.GetDetailAsync(budgetPlanId, GetUserId(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.FinanceReport.Retrieved
        ));
    }

    /// <summary>Exports the finance report cost details for a budget plan.</summary>
    [HttpGet("{budgetPlanId:long}/export")]
    [RequirePermission(Permissions.Report.FinanceReportExport)]
    public async Task<IActionResult> Export(
        long budgetPlanId,
        [FromQuery] string? workOrderId,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var costDetails = await financeReportService.GetCostDetailsForExportAsync(
            budgetPlanId, 
            workOrderId, 
            GetUserId(), 
            ct
        );
        var fileSuffix = string.IsNullOrWhiteSpace(workOrderId) ? budgetPlanId.ToString() : workOrderId;

        await ExportResponseAsync(
            costDetails,
            format,
            FinanceReportExportColumns.Columns,
            $"finance-report-{fileSuffix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Finance Report",
            exportService,
            ct
        );

        return new EmptyResult();
    }
}
