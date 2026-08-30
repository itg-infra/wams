namespace WAMS.Api.Controllers.BudgetPlans;

using WAMS.Api.Controllers.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Rfba;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/budget-plans")]
[Authorize]
public class BudgetPlansController(
    IBudgetPlanService budgetPlanService,
    IValidator<CreateBudgetPlanRequest> createValidator,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions,
    IAuditLogService auditLogService,
    IRfbaFormPdfRenderer rfbaPdfRenderer,
    IPdfMetadataResolver pdfMetadataResolver) : BaseController
{
    private const string TableName = "budget_plans";

    /// <summary>Gets a paginated list of budget plans.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.PlanRead)]
    [ProducesResponseType(typeof(PaginatedResponse<BudgetPlanSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] BudgetPlanQuery query,
        CancellationToken ct)
    {
        var parsedStatus = status is not null ? BudgetPlanStatus.FromValue(status) : null;
        var userId = GetUserId();
        var (items, total) = await budgetPlanService.GetAllAsync(parsedStatus, query, userId, ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            (int)Math.Ceiling(total / (double)query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.BudgetPlan.ListRetrieved
        ));
    }

    /// <summary>Gets a budget plan by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.PlanRead)]
    [ProducesResponseType(typeof(ApiResponse<BudgetPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        long id,
        [FromQuery] long? vendorShadowId,
        CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await budgetPlanService.GetByIdAsync(id, userId, ct, vendorShadowId);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetPlan.Retrieved
        ));
    }

    /// <summary>Creates a new budget plan as a draft.</summary>
    [HttpPost]
    [RequirePermission(Permissions.Budget.PlanCreate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateBudgetPlanRequest request, CancellationToken ct)
    {
        await ValidateRequestAsync(request, ct);

        var userId = GetUserId();
        var result = await budgetPlanService.CreateAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetPlan.Created
        ));
    }

    /// <summary>Creates a new budget plan and immediately submits it for approval.</summary>
    [HttpPost("submit")]
    [RequirePermission(Permissions.Budget.PlanCreate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAndSubmit([FromBody] CreateBudgetPlanRequest request, CancellationToken ct)
    {
        await ValidateRequestAsync(request, ct);

        var userId = GetUserId();
        var result = await budgetPlanService.CreateAndSubmitAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetPlan.CreatedAndSubmitted
        ));
    }

    /// <summary>Updates a budget plan by id.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Budget.PlanUpdate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateBudgetPlanRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await budgetPlanService.UpdateAsync(id, request, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetPlan.Updated
        ));
    }

    /// <summary>Deletes a budget plan by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Budget.PlanDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await budgetPlanService.DeleteAsync(id, ct);

        return NoContent();
    }

    /// <summary>Submits a draft budget plan for approval.</summary>
    [HttpPost("{id:long}/submit")]
    [RequirePermission(Permissions.Budget.PlanSubmit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
    {
        var userId = GetUserId();

        await budgetPlanService.SubmitAsync(id, userId, ct);

        return NoContent();
    }

    /// <summary>Approves a submitted budget plan.</summary>
    [HttpPost("{id:long}/approve")]
    [RequirePermission(Permissions.Budget.PlanApprove)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var roles = GetUserRoles();

        await budgetPlanService.ApproveAsync(id, userId, roles, ct);

        return NoContent();
    }

    /// <summary>Rejects a submitted budget plan.</summary>
    [HttpPost("{id:long}/reject")]
    [RequirePermission(Permissions.Budget.PlanReject)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectBudgetPlanRequest request, CancellationToken ct)
    {
        var userId = GetUserId();

        await budgetPlanService.RejectAsync(id, userId, request, ct);

        return NoContent();
    }

    /// <summary>Adds an SPK item to a budget plan.</summary>
    [HttpPost("{id:long}/spk-items")]
    [RequirePermission(Permissions.Budget.PlanUpdate)]
    [ProducesResponseType(typeof(ApiResponse<BudgetPlanSpkItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddSpkItem(long id, [FromBody] AddSpkItemRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await budgetPlanService.AddSpkItemAsync(id, request, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.BudgetPlan.SpkItemAdded
        ));
    }

    /// <summary>Removes an SPK item from a budget plan.</summary>
    [HttpDelete("{id:long}/spk-items/{spkItemId:long}")]
    [RequirePermission(Permissions.Budget.PlanUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSpkItem(long id, long spkItemId, CancellationToken ct)
    {
        await budgetPlanService.RemoveSpkItemAsync(id, spkItemId, ct);

        return NoContent();
    }

    /// <summary>Exports budget plans to a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.PlanExport)]
    public async Task<IActionResult> Export(
        [FromQuery] BudgetPlanQuery query,
        [FromQuery] string? status = null,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var parsedStatus = status is not null ? BudgetPlanStatus.FromValue(status) : null;
        var stream = budgetPlanService.StreamAllAsync(
            parsedStatus,
            query,
            GetUserId(),
            exportOptions.Value.MaxRows,
            ct
        );

        await StreamExportResponseAsync(
            stream,
            format,
            BudgetPlanExportColumns.Columns,
            $"budget-plans-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Budget Plans",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    private async Task ValidateRequestAsync(CreateBudgetPlanRequest request, CancellationToken ct)
    {
        var result = await createValidator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new WAMS.Domain.Exceptions.ValidationException(errors);
        }
    }

    /// <summary>Gets the paginated change history for a budget plan.</summary>
    [HttpGet("{id:long}/history")]
    [RequirePermission(Permissions.Budget.PlanRead)]
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

    /// <summary>Exports a budget plan's RFBA items as printable Batch Advance forms.</summary>
    /// <remarks>
    /// One A4 page per Bill of Lading, matching the client's reference form. Non-RFBA
    /// items never appear. Plans that are not yet approved print with a DRAFT watermark,
    /// which is what pre-approval review needs.
    /// </remarks>
    [HttpGet("{id:long}/rfba-pdf")]
    [RequirePermission(Permissions.Budget.PlanExport)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportRfbaPdf(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var plan = await budgetPlanService.GetByIdAsync(id, userId, ct);
        var document = RfbaFormMapper.FromBudgetPlan(plan);

        if (document.Pages.Count == 0)
            throw new NotFoundException(ErrorMessages.BudgetPlan.NoRfbaItems(id));

        var metadata = await pdfMetadataResolver.ResolveAsync("RFBA", ct);
        var bytes = rfbaPdfRenderer.Render(document, metadata);

        return File(bytes, "application/pdf", $"RFBA-{plan.BudgetNo}.pdf");
    }
}
