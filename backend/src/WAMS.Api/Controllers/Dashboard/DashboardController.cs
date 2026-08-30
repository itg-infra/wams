namespace WAMS.Api.Controllers.Dashboard;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Dashboard;
using WAMS.Application.Interfaces.Dashboard;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController(IDashboardService dashboardService) : BaseController
{
    /// <summary>Gets the dashboard summary for the current user.</summary>
    [HttpGet("summary")]
    [RequirePermission(Permissions.Report.DashboardRead)]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var result = await dashboardService.GetSummaryAsync(GetUserId(), GetUserRoles(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.Dashboard.SummaryRetrieved
        ));
    }

    /// <summary>Gets a paginated list of today's activities for the current user.</summary>
    [HttpGet("activities")]
    [RequirePermission(Permissions.Report.DashboardRead)]
    [ProducesResponseType(typeof(PaginatedResponse<DashboardActivityResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivities([FromQuery] DashboardActivityQuery query, CancellationToken ct)
    {
        var (items, total) = await dashboardService.GetTodayActivitiesAsync(query, GetUserId(), ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.Dashboard.ActivitiesRetrieved
        ));
    }

    /// <summary>Gets dashboard history for a given year and month.</summary>
    [HttpGet("history")]
    [RequirePermission(Permissions.Report.DashboardRead)]
    [ProducesResponseType(typeof(ApiResponse<DashboardHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var result = await dashboardService.GetHistoryAsync(year, month, GetUserId(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.Dashboard.HistoryRetrieved
        ));
    }
}
