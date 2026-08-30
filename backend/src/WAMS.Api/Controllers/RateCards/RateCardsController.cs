namespace WAMS.Api.Controllers.RateCards;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.Common;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.RateCards;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Domain.Enums;

[ApiController]
[Route("api/v1/rate-cards")]
[Authorize]
public class RateCardsController(IRateCardService rateCardService, IRateCardRepository rateCardRepo, IExportService exportService, IOptions<ExportOptions> exportOptions, IPphLookupService pphLookupService) : BaseController
{
    /// <summary>
    /// Returns all vendors that have a submitted rate card covering the given item.
    /// Used by the Budget Plan create form to populate the vendor dropdown per line item.
    /// </summary>
    [HttpGet("by-item/{itemShadowId:long}")]
    [RequirePermission(Permissions.Budget.RateCardRead)]
    [ProducesResponseType(typeof(ApiResponse<List<VendorRateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVendorRatesForItem(long itemShadowId, CancellationToken ct)
    {
        var items = await rateCardRepo.GetSubmittedRatesForItemAsync(itemShadowId, ct);
        var result = items.Select(i => new VendorRateResponse(
            i.RateCard.VendorShadowId,
            i.RateCard.Vendor.CardCode,
            i.RateCard.Vendor.CardName,
            i.UomMasterId,
            i.Uom.Code,
            i.Uom.Name,
            i.CostValue,
            i.PpnTaxTypeId is { } ppnId ? new RateCardItemTaxResponse(ppnId, i.PpnTaxTypeCode!, i.PpnRate ?? 0m) : null,
            i.PphTaxTypeId is { } pphId ? new RateCardItemTaxResponse(pphId, i.PphTaxTypeCode!, i.PphRate ?? 0m) : null,
            i.CostTreatment)).ToList();

        return Ok(OkResponse(
            result,
            SuccessMessages.RateCard.VendorRatesRetrieved
        ));
    }

    /// <summary>
    /// Returns SAP's currently-assigned withholding-tax (PPh) codes for a vendor, refreshing
    /// from SAP live on every call. Used by the RateCard create/edit form to pre-select a
    /// default PPh code once the admin picks a vendor - the admin can still override.
    /// </summary>
    [HttpGet("vendors/{vendorId:long}/pph")]
    [RequirePermission(Permissions.Budget.RateCardRead)]
    [ProducesResponseType(typeof(ApiResponse<List<WAMS.Application.DTOs.TaxTypes.TaxTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPphSuggestions(long vendorId, CancellationToken ct)
    {
        var result = await pphLookupService.GetOrRefreshAsync(vendorId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.TaxType.ListRetrieved
        ));
    }

    /// <summary>Exports rate cards matching the given filters.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.Budget.RateCardExport)]
    public async Task<IActionResult> Export(
        [FromQuery] string? status,
        [FromQuery] long? vendorId,
        [FromQuery] DataTableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var parsedStatus = status is not null ? RateCardStatus.FromValue(status) : null;
        var stream = rateCardService.StreamAllAsync(
            parsedStatus, 
            vendorId, 
            query, 
            exportOptions.Value.MaxRows, 
            ct
        );

        await StreamExportResponseAsync(
            stream,
            format,
            RateCardExportColumns.Columns,
            $"rate-cards-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Rate Cards",
            exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>Gets a paginated list of rate cards.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.RateCardRead)]
    [ProducesResponseType(typeof(PaginatedResponse<RateCardSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] long? vendorId,
        [FromQuery] DataTableQuery query,
        CancellationToken ct)
    {
        var parsedStatus = status is not null ? RateCardStatus.FromValue(status) : null;
        var result = await rateCardService.GetAllAsync(parsedStatus, vendorId, query, ct);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.RateCard.ListRetrieved
        ));
    }

    /// <summary>Gets a rate card by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.RateCardRead)]
    [ProducesResponseType(typeof(ApiResponse<RateCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await rateCardService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RateCard.Retrieved
        ));
    }

    /// <summary>Creates a new rate card.</summary>
    [HttpPost]
    [RequirePermission(Permissions.Budget.RateCardCreate)]
    [ProducesResponseType(typeof(ApiResponse<RateCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateRateCardRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await rateCardService.CreateAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RateCard.Created
        ));
    }

    /// <summary>Creates a new rate card and immediately submits it.</summary>
    [HttpPost("submit")]
    [RequirePermission(Permissions.Budget.RateCardCreate)]
    [RequirePermission(Permissions.Budget.RateCardSubmit)]
    [ProducesResponseType(typeof(ApiResponse<RateCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateAndSubmit([FromBody] CreateRateCardRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await rateCardService.CreateAndSubmitAsync(userId, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RateCard.CreatedAndSubmitted
        ));
    }

    /// <summary>Updates an existing rate card.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Budget.RateCardUpdate)]
    [ProducesResponseType(typeof(ApiResponse<RateCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRateCardRequest request, CancellationToken ct)
    {
        var result = await rateCardService.UpdateAsync(id, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RateCard.Updated
        ));
    }

    /// <summary>Deletes a rate card by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Budget.RateCardDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await rateCardService.DeleteAsync(id, ct);

        return NoContent();
    }

    /// <summary>Submits an existing rate card.</summary>
    [HttpPost("{id:long}/submit")]
    [RequirePermission(Permissions.Budget.RateCardSubmit)]
    [ProducesResponseType(typeof(ApiResponse<RateCardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await rateCardService.SubmitAsync(id, userId, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.RateCard.Submitted
        ));
    }
}
