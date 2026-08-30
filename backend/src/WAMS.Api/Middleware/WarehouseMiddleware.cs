using WAMS.Application.Interfaces.Warehouses;

namespace WAMS.Api.Middleware;

internal static class WarehouseMiddlewareConstants
{
    public const string HeaderName = "X-Warehouse-Id";
    public const string PermissionsClaimType = "permissions";
    public const string SuperAdminPermission = "*.*.*";
}

public class WarehouseMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IWarehouseContext warehouseContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var isSuperAdmin = context.User.Claims
                .Any(c => c.Type == WarehouseMiddlewareConstants.PermissionsClaimType
                       && c.Value == WarehouseMiddlewareConstants.SuperAdminPermission);

            if (TryParseWarehouseId(context, out var warehouseId))
            {
                warehouseContext.SetWarehouseId(warehouseId);
            }
            else if (isSuperAdmin)
            {
                warehouseContext.SetBypassMode();
            }
        }

        await _next(context);
    }

    private static bool TryParseWarehouseId(HttpContext context, out long warehouseId)
    {
        warehouseId = 0;

        if (!context.Request.Headers.TryGetValue(WarehouseMiddlewareConstants.HeaderName, out var headerValues))
        {
            return false;
        }

        var headerValue = headerValues.FirstOrDefault()?.Trim();

        if (string.IsNullOrEmpty(headerValue) || !long.TryParse(headerValue, out warehouseId))
        {
            return false;
        }

        return true;
    }
}
