namespace WAMS.Api.Controllers.BudgetTemplates;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.BudgetTemplates;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Domain.Enums;

[ApiController]
[Route("api/v1/budget-templates")]
[Authorize]
public class BudgetTemplatesController(
    IBudgetTemplateService budgetTemplateService,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions,
    IAuditLogService auditLogService) : BaseController
{
    private const string TableName = "budget_templates";

    /// <summary>Exports budget templates to a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.TemplateExport)]
    public async Task<IActionResult> Export(
        [FromQuery] string? status,
        [FromQuery] BudgetTemplateQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var parsedStatus = status is not null ? BudgetTemplateStatus.FromValue(status) : null;
        var stream = budgetTemplateService.StreamAllAsync(
            parsedStatus,
            query,
            GetUserId(),
            exportOptions.Value.MaxRows,
            ct
        );

        await StreamExportResponseAsync(
            stream,
            format,
            BudgetTemplateExportColumns.Columns,
            $"budget-templates-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Budget Templates",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets a paginated list of budget templates.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.TemplateRead)]
    [ProducesResponseType(typeof(PaginatedResponse<BudgetTemplateSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] BudgetTemplateQuery query,
        CancellationToken ct)
    {
        var parsedStatus = status is not null ? BudgetTemplateStatus.FromValue(status) : null;
        var userId = GetUserId();
        var (items, total) = await budgetTemplateService.GetAllAsync(
            parsedStatus,
            query,
            userId,
            ct
        );
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.BudgetTemplate.ListRetrieved
        ));
    }

    /// <summary>Gets a budget template by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.TemplateRead)]
    [ProducesResponseType(typeof(ApiResponse<BudgetTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await budgetTemplateService.GetByIdAsync(id, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetTemplate.Retrieved
        ));
    }

    /// <summary>Creates a new budget template as a draft.</summary>
    [HttpPost]
    [RequirePermission(Permissions.Budget.TemplateCreate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetTemplateRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await budgetTemplateService.CreateAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetTemplate.Created
        ));
    }

    /// <summary>Creates a new budget template and immediately submits it for approval.</summary>
    [HttpPost("submit")]
    [RequirePermission(Permissions.Budget.TemplateCreate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAndSubmit([FromBody] CreateBudgetTemplateRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await budgetTemplateService.CreateAndSubmitAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetTemplate.CreatedAndSubmitted
        ));
    }

    /// <summary>Updates a budget template by id.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Budget.TemplateUpdate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBudgetTemplateRequest request, CancellationToken ct)
    {
        var result = await budgetTemplateService.UpdateAsync(id, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetTemplate.Updated
        ));
    }

    /// <summary>Deletes a budget template by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Budget.TemplateDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await budgetTemplateService.DeleteAsync(id, ct);

        return NoContent();
    }

    /// <summary>Submits a draft budget template for approval.</summary>
    [HttpPost("{id:long}/submit")]
    [RequirePermission(Permissions.Budget.TemplateSubmit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
    {
        var userId = GetUserId();

        await budgetTemplateService.SubmitAsync(id, userId, ct);

        return NoContent();
    }

    /// <summary>Gets the paginated change history for a budget template.</summary>
    [HttpGet("{id:long}/history")]
    [RequirePermission(Permissions.Budget.TemplateRead)]
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
