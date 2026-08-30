namespace WAMS.Api.Controllers.ActivityTypes;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.ActivityTypes;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Interfaces.ActivityTypes;

[ApiController]
[Route("api/v1/activity-types")]
[Authorize]
public class ActivityTypesController(IActivityTypeService activityTypeService) : BaseController
{
    /// <summary>Gets all activity types.</summary>
    [HttpGet]
    [RequirePermission(Permissions.Budget.TemplateRead)]
    [ProducesResponseType(typeof(ApiResponse<List<ActivityTypeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await activityTypeService.GetAllAsync(ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.ActivityType.ListRetrieved
        ));
    }

    /// <summary>Gets a single activity type by id.</summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Budget.TemplateRead)]
    [ProducesResponseType(typeof(ApiResponse<ActivityTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await activityTypeService.GetByIdAsync(id, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.ActivityType.Retrieved
        ));
    }

    /// <summary>Creates a new activity type.</summary>
    [HttpPost]
    [RequirePermission(Permissions.System.ActivityTypeCreate)]
    [ProducesResponseType(typeof(ApiResponse<ActivityTypeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateActivityTypeRequest request, CancellationToken ct)
    {
        var result = await activityTypeService.CreateAsync(request, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            OkResponse(
                result,
                SuccessMessages.ActivityType.Created
            )
        );
    }

    /// <summary>Updates an existing activity type.</summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.System.ActivityTypeUpdate)]
    [ProducesResponseType(typeof(ApiResponse<ActivityTypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateActivityTypeRequest request, CancellationToken ct)
    {
        var result = await activityTypeService.UpdateAsync(id, request, ct);

        return Ok(OkResponse(
            result,
            SuccessMessages.ActivityType.Updated
        ));
    }

    /// <summary>Deletes an activity type.</summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.System.ActivityTypeDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await activityTypeService.DeleteAsync(id, ct);

        return NoContent();
    }
}
