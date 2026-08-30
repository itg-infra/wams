namespace WAMS.Api.Controllers.AccountPayables;

using WAMS.Api.Controllers.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using Microsoft.Extensions.Options;
using WAMS.Application.Common;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Application.DTOs.AuditLogs;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.AccountPayables;
using WAMS.Application.Interfaces.AuditLogs;

[ApiController]
[Route("api/v1/account-payables")]
[Authorize]
public class AccountPayablesController(
    IAccountPayableService apService,
    IValidator<CreateAccountPayableRequest> createValidator,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions,
    IAuditLogService auditLogService) : BaseController
{
    private const string TableName = "account_payables";

    /// <summary>Gets paginated approved work order recaps eligible for account payable creation.</summary>
    [HttpGet("approved-recaps")]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
    [ProducesResponseType(typeof(PaginatedResponse<ApprovedRecapApStatusResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApprovedRecaps([FromQuery] DataTableQuery query, CancellationToken ct)
    {
        var (items, total) = await apService.GetApprovedRecapsAsync(
            GetUserId(),
            query.Page,
            query.Limit,
            ct
        );
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            GetPageCount(total, query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.AccountPayable.ApprovedRecapsRetrieved
        ));
    }

    /// <summary>Gets a paginated list of account payables.</summary>
    [HttpGet]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
    [ProducesResponseType(typeof(PaginatedResponse<AccountPayableSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AccountPayableQuery query, CancellationToken ct)
    {
        var (items, total) = await apService.GetAllAsync(query, ct);
        var meta = new PaginationMeta(
            query.Page,
            query.Limit,
            total,
            GetPageCount(total, query.Limit)
        );

        return Ok(OkPaginatedResponse(
            items,
            meta,
            SuccessMessages.AccountPayable.ListRetrieved
        ));
    }

    /// <summary>Gets a single account payable by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
    [ProducesResponseType(typeof(ApiResponse<AccountPayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await apService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.AccountPayable.Retrieved
        ));
    }

    /// <summary>Gets items available for account payable creation for a vendor within given budget plans.</summary>
    /// <remarks>
    /// Response rows carry two independent signals, do not conflate them:
    /// <c>isGenerated</c> is true only when the item sits on an AP with <c>Status == Generated</c>;
    /// it says nothing about Draft APs holding the item.
    /// <c>takenByCode</c> is the Code of whichever AP (Draft or Generated) currently holds the item,
    /// or null if it is genuinely free. <c>takenByCode == null</c> is the selectability signal an
    /// item can be picked; <c>isGenerated == false</c> alone does NOT mean an item is free, it may
    /// still be sitting on someone else's Draft AP (non-null <c>takenByCode</c>).
    /// When <c>includeGenerated=false</c>, every returned row already has <c>takenByCode == null</c>
    /// by construction (taken items are filtered out before the response is built).
    /// PO and AP hold items in independent pools: an item on a Draft PO is still selectable here.
    /// </remarks>
    /// <param name="vendorShadowId">The vendor to find available budget plan items for.</param>
    /// <param name="budgetPlanIds">Comma-separated list of budget plan ids to search within.</param>
    /// <param name="includeGenerated">When true, also includes items already taken by another AP (Draft or Generated), each flagged via isGenerated/takenByCode.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("available-items")]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
    [ProducesResponseType(typeof(ApiResponse<List<AvailableApItemResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableItems(
        [FromQuery] long vendorShadowId,
        [FromQuery] string? budgetPlanIds,
        [FromQuery] bool includeGenerated = false,
        CancellationToken ct = default)
    {
        var ids = ParseBudgetPlanIds(budgetPlanIds);
        var result = await apService.GetAvailableItemsByBudgetPlansAsync(
            GetUserId(),
            vendorShadowId,
            ids,
            includeGenerated,
            null,
            ct
        );

        return Ok(OkResponse(
            result,
            SuccessMessages.General.AvailableItemsRetrieved
        ));
    }

    /// <summary>Gets available items while editing the specified draft account payable.</summary>
    [HttpGet("{accountPayableId:long}/available-items")]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
    public async Task<IActionResult> GetAvailableItemsForEdit(
        long accountPayableId,
        [FromQuery] long vendorShadowId,
        [FromQuery] string? budgetPlanIds,
        [FromQuery] bool includeGenerated = false,
        CancellationToken ct = default)
    {
        var ids = ParseBudgetPlanIds(budgetPlanIds);
        var result = await apService.GetAvailableItemsByBudgetPlansAsync(
            GetUserId(), vendorShadowId, ids, includeGenerated, accountPayableId, ct);

        return Ok(OkResponse(result, SuccessMessages.General.AvailableItemsRetrieved));
    }

    /// <summary>Previews computed totals for an account payable before creating it.</summary>
    [HttpPost("preview")]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
    [ProducesResponseType(typeof(ApiResponse<AccountPayableTotalsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview([FromBody] PreviewAccountPayableRequest request, CancellationToken ct)
    {
        var result = await apService.PreviewAsync(GetUserId(), request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.AccountPayable.Retrieved
        ));
    }

    /// <summary>Creates a new account payable as a draft.</summary>
    [HttpPost]
    [RequirePermission(Permissions.WorkOrder.ApCreate)]
    [ProducesResponseType(typeof(ApiResponse<AccountPayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountPayableRequest request,
        CancellationToken ct)
    {
        await ValidateCreateRequestAsync(request, ct);
        var result = await apService.CreateAsync(GetUserId(), request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.AccountPayable.Created
        ));
    }

    /// <summary>Creates a new account payable and immediately generates it in SAP.</summary>
    [HttpPost("generate")]
    [RequirePermission(Permissions.WorkOrder.ApGenerate)]
    [ProducesResponseType(typeof(ApiResponse<AccountPayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAndGenerate(
        [FromBody] CreateAccountPayableRequest request,
        CancellationToken ct)
    {
        await ValidateCreateRequestAsync(request, ct);
        var result = await apService.CreateAndGenerateAsync(GetUserId(), request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.AccountPayable.CreatedAndGenerated
        ));
    }

    private static int GetPageCount(long total, int limit) => (int)Math.Ceiling(total / (double)limit);

    private List<long> ParseBudgetPlanIds(string? budgetPlanIds)
    {
        var ids = new List<long>();
        if (string.IsNullOrWhiteSpace(budgetPlanIds))
            return ids;

        foreach (var part in budgetPlanIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (!long.TryParse(trimmed, out var parsedId) || parsedId <= 0)
            {
                throw new Domain.Exceptions.ValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["budgetPlanIds"] = [string.Format(ErrorCodes.InvalidBudgetPlanId, trimmed)]
                    });
            }

            ids.Add(parsedId);
        }

        return ids;
    }

    private async Task ValidateCreateRequestAsync(CreateAccountPayableRequest request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            throw new Domain.Exceptions.ValidationException(errors);
        }
    }

    /// <summary>Updates an existing draft account payable.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.ApUpdate)]
    [ProducesResponseType(typeof(ApiResponse<AccountPayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateAccountPayableRequest request,
        CancellationToken ct)
    {
        var result = await apService.UpdateAsync(id, GetUserId(), request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.AccountPayable.Updated
        ));
    }

    /// <summary>Deletes a draft account payable.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.WorkOrder.ApDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await apService.DeleteAsync(id, ct);

        return NoContent();
    }

    /// <summary>Generates an existing draft account payable in SAP.</summary>
    [HttpPost("{id:long}/generate")]
    [RequirePermission(Permissions.WorkOrder.ApGenerate)]
    [ProducesResponseType(typeof(ApiResponse<AccountPayableResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Generate(long id, CancellationToken ct)
    {
        var result = await apService.GenerateAsync(id, GetUserId(), ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.AccountPayable.Generated
        ));
    }

    /// <summary>Exports account payables matching the query as a file stream.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.WorkOrder.ApExport)]
    public async Task<IActionResult> Export(
        [FromQuery] AccountPayableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = apService.StreamAllAsync(query, exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            AccountPayableExportColumns.Columns,
            $"account-payables-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Account Payables",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets the audit history of an account payable.</summary>
    [HttpGet("{id:long}/history")]
    [RequirePermission(Permissions.WorkOrder.ApRead)]
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
