namespace WAMS.Api.Controllers.Roles;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Interfaces.Rbac;

[ApiController]
[Route("api/v1/permissions")]
[Authorize]
public class PermissionsController(IRbacService rbacService) : BaseController
{
    private readonly IRbacService _rbacService = rbacService;

    /// <summary>
    /// List all permissions
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.User.PermissionRead)]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionInfo>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _rbacService.GetAllPermissionsAsync();

        return Ok(OkResponse(
            result,
            SuccessMessages.Permission.ListRetrieved
        ));
    }
}
