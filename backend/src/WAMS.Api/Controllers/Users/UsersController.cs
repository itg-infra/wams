namespace WAMS.Api.Controllers.Users;

using WAMS.Api.Controllers.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using WAMS.Api.Filters;
using WAMS.Domain.Constants;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Roles;
using WAMS.Application.DTOs.Users;
using WAMS.Application.Export;
using WAMS.Application.Export.Definitions;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Common;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UsersController(
    IUserService userService,
    IRbacService rbacService,
    IValidator<CreateUserRequest> createUserValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator,
    IExportService exportService,
    IOptions<ExportOptions> exportOptions) : BaseController
{
    private readonly IUserService _userService = userService;
    private readonly IRbacService _rbacService = rbacService;
    private readonly IValidator<CreateUserRequest> _createUserValidator = createUserValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator = resetPasswordValidator;
    private readonly IExportService _exportService = exportService;
    private readonly IOptions<ExportOptions> _exportOptions = exportOptions;

    /// <summary>
    /// List all users with pagination
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.User.Read)]
    [ProducesResponseType(typeof(PaginatedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] DataTableQuery query)
    {
        var result = await _userService.GetAllAsync(query);

        return Ok(OkPaginatedResponse(
            result.Data,
            result.Meta,
            SuccessMessages.User.ListRetrieved
        ));
    }

    /// <summary>Exports users matching the query to a file stream.</summary>
    [HttpGet("export")]
    [RequirePermission(Permissions.User.Export)]
    public async Task<IActionResult> Export(
        [FromQuery] DataTableQuery query,
        [FromQuery] ExportFormat format = ExportFormat.Xlsx,
        CancellationToken ct = default)
    {
        var stream = _userService.StreamAllAsync(query, _exportOptions.Value.MaxRows, ct);

        await StreamExportResponseAsync(
            stream,
            format,
            UserExportColumns.Columns,
            $"users-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            "Users",
            _exportService,
            ct
        );

        return new EmptyResult();
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.User.Read)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _userService.GetByIdAsync(id);

        return Ok(OkResponse(
            result,
            SuccessMessages.User.Retrieved
        ));
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.User.Create)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var validation = await _createUserValidator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var createdBy = GetUserId();
        var result = await _userService.CreateAsync(request, createdBy);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            OkResponse(
                result,
                SuccessMessages.User.Created
            )
        );
    }

    /// <summary>
    /// Update user information
    /// </summary>
    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.User.Update)]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request)
    {
        var result = await _userService.UpdateAsync(id, request);

        return Ok(OkResponse(
            result,
            SuccessMessages.User.Updated
        ));
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.User.Delete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        await _userService.DeleteAsync(id);

        return Ok(OkResponse(SuccessMessages.User.Deleted));
    }

    /// <summary>
    /// Reset a user's password (admin action, does not require the target's current password)
    /// </summary>
    [HttpPost("{id:long}/password")]
    [RequirePermission(Permissions.User.ResetPassword)]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword(long id, [FromBody] ResetPasswordRequest request)
    {
        var validation = await _resetPasswordValidator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var actorUserId = GetUserId();
        await _userService.ResetPasswordAsync(id, request, actorUserId);

        return Ok(OkResponse(SuccessMessages.User.PasswordChanged));
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    [HttpPost("{id:long}/roles/{roleId:long}")]
    [RequirePermission(Permissions.User.RoleCreate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(long id, long roleId)
    {
        await _userService.AssignRoleAsync(id, new AssignRoleRequest(roleId));

        return Ok(OkResponse(SuccessMessages.User.RoleAssigned));
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
    [HttpDelete("{id:long}/roles/{roleId:long}")]
    [RequirePermission(Permissions.User.RoleDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(long id, long roleId)
    {
        await _userService.RemoveRoleAsync(id, roleId);

        return Ok(OkResponse(SuccessMessages.User.RoleRemoved));
    }

    /// <summary>
    /// Assign warehouse to user
    /// </summary>
    [HttpPost("{id:long}/warehouses/{warehouseId:long}")]
    [RequirePermission(Permissions.User.WarehouseCreate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignWarehouse(long id, long warehouseId, [FromBody] AssignWarehouseRequest? body = null)
    {
        await _userService.AssignWarehouseAsync(id, new AssignWarehouseRequest(warehouseId, body?.IsPrimary ?? false));

        return Ok(OkResponse(SuccessMessages.User.WarehouseAssigned));
    }

    /// <summary>
    /// Remove warehouse from user
    /// </summary>
    [HttpDelete("{id:long}/warehouses/{warehouseId:long}")]
    [RequirePermission(Permissions.User.WarehouseDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveWarehouse(long id, long warehouseId)
    {
        await _userService.RemoveWarehouseAsync(id, warehouseId);

        return Ok(OkResponse(SuccessMessages.User.WarehouseRemoved));
    }

    // User-level permission overrides
    /// <summary>
    /// List all permission overrides for a user
    /// </summary>
    [HttpGet("{id:long}/permissions")]
    [RequirePermission(Permissions.User.PermissionRead)]
    [ProducesResponseType(typeof(ApiResponse<List<UserPermissionOverrideResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionOverrides(long id)
    {
        var result = await _rbacService.GetUserPermissionOverridesAsync(id);

        return Ok(OkResponse(
            result,
            SuccessMessages.User.PermissionOverridesRetrieved
        ));
    }

    /// <summary>
    /// Grant an extra permission to a user (beyond their roles)
    /// </summary>
    [HttpPost("{id:long}/permissions/{permissionId:long}/grant")]
    [RequirePermission(Permissions.User.PermissionCreate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GrantPermission(long id, long permissionId, [FromBody] UserPermissionOverrideRequest request)
    {
        await _rbacService.GrantUserPermissionAsync(id, permissionId, request, GetUserId());

        return Ok(OkResponse(SuccessMessages.User.PermissionGranted));
    }

    /// <summary>
    /// Explicitly deny a permission for a user (overrides role grants)
    /// </summary>
    [HttpPost("{id:long}/permissions/{permissionId:long}/deny")]
    [RequirePermission(Permissions.User.PermissionCreate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DenyPermission(long id, long permissionId, [FromBody] UserPermissionOverrideRequest request)
    {
        await _rbacService.DenyUserPermissionAsync(id, permissionId, request, GetUserId());

        return Ok(OkResponse(SuccessMessages.User.PermissionDenied));
    }

    /// <summary>
    /// Remove a user-level permission override (reverts to role default)
    /// </summary>
    [HttpDelete("{id:long}/permissions/{permissionId:long}")]
    [RequirePermission(Permissions.User.PermissionDelete)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemovePermissionOverride(long id, long permissionId)
    {
        await _rbacService.RemoveUserPermissionAsync(id, permissionId);

        return Ok(OkResponse(SuccessMessages.User.PermissionOverrideRemoved));
    }

    /// <summary>
    /// Get all effective permissions for a user (role grants + user overrides resolved)
    /// </summary>
    [HttpGet("{id:long}/permissions/effective")]
    [RequirePermission(Permissions.User.PermissionRead)]
    [ProducesResponseType(typeof(ApiResponse<List<EffectivePermissionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffectivePermissions(long id)
    {
        var result = await _rbacService.GetEffectivePermissionsAsync(id);

        return Ok(OkResponse(
            result,
            SuccessMessages.User.EffectivePermissionsRetrieved
        ));
    }
}
