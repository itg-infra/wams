namespace WAMS.Api.Filters;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.Filters;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Domain.Constants;
using WAMS.Domain.Exceptions;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const int PermissionPartCount = 3;
    private readonly string _module;
    private readonly string _resource;
    private readonly string _action;
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
        (_module, _resource, _action) = ParsePermission(permission);
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user.Identity == null || !user.Identity.IsAuthenticated)
            throw new UnauthorizedException(ErrorMessages.Permission.AuthenticationRequired);

        var jti = user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (!string.IsNullOrEmpty(jti))
        {
            var tokenService = GetService<ITokenService>(context);
            if (await tokenService.IsTokenBlacklistedAsync(jti))
                throw new UnauthorizedException(ErrorMessages.Permission.TokenRevoked);
        }

        var subClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!long.TryParse(subClaim, out var userId))
            throw new UnauthorizedException(ErrorMessages.Permission.InvalidTokenSubject);

        var rbacService = GetService<IRbacService>(context);
        var hasPermission = await rbacService.HasPermissionAsync(userId, _module, _resource, _action);

        if (!hasPermission)
            throw new ForbiddenException(ErrorMessages.Permission.MissingPermission(_permission));
    }

    private static (string module, string resource, string action) ParsePermission(string permission)
    {
        var parts = permission.Split('.');

        if (parts.Length != PermissionPartCount)
            throw new ArgumentException(ErrorMessages.Permission.InvalidPermissionKey(permission), nameof(permission));

        return (parts[0], parts[1], parts[2]);
    }

    private static T GetService<T>(AuthorizationFilterContext context) where T : notnull =>
        context.HttpContext.RequestServices.GetRequiredService<T>();
}
