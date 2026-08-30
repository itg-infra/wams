namespace WAMS.Api.Controllers.Uoms;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.Interfaces.Uoms;

[ApiController]
[Route("api/v1/uoms")]
[Authorize]
public class UomsController(IUomService uomService) : BaseController
{
    /// <summary>Gets a list of units of measure.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.UomRead)]
    [ProducesResponseType(typeof(ApiResponse<List<UomResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var result = await uomService.GetAllAsync(activeOnly, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.Uom.ListRetrieved
        ));
    }

    /// <summary>Gets a unit of measure by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.UomRead)]
    [ProducesResponseType(typeof(ApiResponse<UomResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await uomService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.Uom.Retrieved
        ));
    }

    /// <summary>Creates a new unit of measure.</summary>
    [HttpPost]
    [RequirePermission(Permissions.Budget.UomCreate)]
    [ProducesResponseType(typeof(ApiResponse<UomResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUomRequest request, CancellationToken ct)
    {
        var result = await uomService.CreateAsync(request, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            OkResponse(
                result,
                SuccessMessages.Uom.Created
            )
        );
    }

    /// <summary>Updates an existing unit of measure.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.Budget.UomUpdate)]
    [ProducesResponseType(typeof(ApiResponse<UomResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUomRequest request, CancellationToken ct)
    {
        var result = await uomService.UpdateAsync(id, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.Uom.Updated
        ));
    }

    /// <summary>Deletes a unit of measure by id.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Budget.UomDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await uomService.DeleteAsync(id, ct);

        return Ok(OkResponse(SuccessMessages.Uom.Deleted));
    }
}
