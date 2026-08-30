namespace WAMS.Api.Controllers.Roles;

using WAMS.Api.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Common;

[ApiController]
[Route("api/v1/roles")]
[Authorize]
public class RolesController : BaseController
{
    private readonly IRbacService _rbacService;
    private readonly IExportService _exportService;
    private readonly IOptions<ExportOptions> _exportOptions;

    public RolesController(IRbacService rbacService, IExportService exportService, IOptions<ExportOptions> exportOptions)
    {
        _rbacService = rbacService;
        _exportService = exportService;
        _exportOptions = exportOptions;
    }

    /// <summary>Exports roles as a file in the requested format.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.User.RoleExport)]
    public async Task<IActionResult> Export(
        [FromQuery] DataTableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = _rbacService.StreamAllRolesAsync(query, _exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            RoleExportColumns.Columns,
            $"roles-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Roles",
            _exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>
    /// List all roles
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.User.RoleRead)]
    [ProducesResponseType(typeof(PaginatedResponse<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DataTableQuery query)
    {
        var result = await _rbacService.GetAllRolesAsync(query);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.Role.ListRetrieved
        ));
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.User.RoleRead)]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _rbacService.GetRoleByIdAsync(id);

        return Ok(OkResponse(
            result,
            SuccessMessages.Role.Retrieved
        ));
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.User.RoleCreate)]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request)
    {
        var result = await _rbacService.CreateRoleAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            OkResponse(
                result,
                SuccessMessages.Role.Created
            )
        );
    }

    /// <summary>
    /// Update role information
    /// </summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.User.RoleUpdate)]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _rbacService.UpdateRoleAsync(id, request);

        return Ok(OkResponse(
            result,
            SuccessMessages.Role.Updated
        ));
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.User.RoleDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(long id)
    {
        await _rbacService.DeleteRoleAsync(id);

        return Ok(OkResponse(SuccessMessages.Role.Deleted));
    }

    /// <summary>
    /// Sync (replace) all permissions for a role in one call
    /// </summary>
    [HttpPut("{id:long}/permissions")]
    [RequirePermission(Permissions.User.RoleUpdate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncPermissions(long id, [FromBody] SyncPermissionsRequest request)
    {
        var userId = GetUserId();
        await _rbacService.SyncPermissionsAsync(id, request, userId);

        return Ok(OkResponse(SuccessMessages.Role.PermissionsUpdated));
    }

    /// <summary>
    /// Assign permission to role
    /// </summary>
    [HttpPost("{id:long}/permissions/{permissionId:long}")]
    [RequirePermission(Permissions.User.RoleUpdate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignPermission(long id, long permissionId)
    {
        var userId = GetUserId();
        await _rbacService.AssignPermissionAsync(id, new AssignPermissionRequest(permissionId), userId);

        return Ok(OkResponse(SuccessMessages.Role.PermissionAssigned));
    }

    /// <summary>
    /// Remove permission from role
    /// </summary>
    [HttpDelete("{id:long}/permissions/{permissionId:long}")]
    [RequirePermission(Permissions.User.RoleUpdate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermission(long id, long permissionId)
    {
        await _rbacService.RemovePermissionAsync(id, permissionId);

        return Ok(OkResponse(SuccessMessages.Role.PermissionRemoved));
    }

}
