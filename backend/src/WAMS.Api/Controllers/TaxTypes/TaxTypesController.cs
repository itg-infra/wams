namespace WAMS.Api.Controllers.TaxTypes;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.TaxTypes;
using WAMS.Application.Interfaces.TaxTypes;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;

[ApiController]
[Route("api/v1/tax-types")]
[Authorize]
public class TaxTypesController(ITaxTypeService taxTypeService) : BaseController
{
    /// <summary>Gets a list of tax types, optionally filtered by category and active status.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.TaxTypeRead)]
    [ProducesResponseType(typeof(ApiResponse<List<TaxTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category, 
        [FromQuery] bool activeOnly = true, 
        CancellationToken ct = default
    )
    {
        TaxCategory? parsedCategory = null;

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!TaxCategory.TryFromName(category, true, out var found)) throw new ValidationException(ErrorMessages.Validation.TaxType.CategoryInvalid(category));
            parsedCategory = found;
        }

        var result = await taxTypeService.GetAllAsync(parsedCategory, activeOnly, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.TaxType.ListRetrieved
        ));
    }

    /// <summary>Gets a tax type by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.TaxTypeRead)]
    [ProducesResponseType(typeof(ApiResponse<TaxTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await taxTypeService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.TaxType.Retrieved
        ));
    }
}
