namespace WAMS.Api.Controllers.Auth;

using WAMS.Api.Controllers.Common;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WAMS.Application.DTOs.Auth;
using WAMS.Application.DTOs.Common;
using WAMS.Application.Interfaces.Auth;
using WAMS.Domain.Constants;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<LoginRequest> loginValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator) : BaseController
{
    private readonly IAuthService _authService = authService;
    private readonly IValidator<LoginRequest> _loginValidator = loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator = changePasswordValidator;

    /// <summary>
    /// Authenticate user and receive access + refresh tokens
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var deviceInfo = HttpContext.Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request, ipAddress, deviceInfo);

        return Ok(OkResponse(
            result,
            SuccessMessages.Auth.LoginSuccessful
        ));
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshAsync(request);

        return Ok(OkResponse(
            result,
            SuccessMessages.Auth.TokenRefreshed
        ));
    }

    /// <summary>
    /// Logout and invalidate tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var userId = GetUserId();
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;

        await _authService.LogoutAsync(userId, jti, request.RefreshToken);

        return Ok(OkResponse(SuccessMessages.Auth.LoggedOut));
    }

    /// <summary>
    /// Get current authenticated user info
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<MeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var userId = GetUserId();
        var result = await _authService.GetCurrentUserAsync(userId);

        return Ok(OkResponse(
            result,
            SuccessMessages.User.Retrieved
        ));
    }

    /// <summary>
    /// Change own password (requires current password)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request);

        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Domain.Exceptions.ValidationException(errors);
        }

        var userId = GetUserId();

        await _authService.ChangePasswordAsync(userId, request);

        return Ok(OkResponse(SuccessMessages.User.PasswordChanged));
    }

}
