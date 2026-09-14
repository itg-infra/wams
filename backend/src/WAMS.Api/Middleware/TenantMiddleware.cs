using WAMS.Application.Interfaces.Common;

namespace WAMS.Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var companyIdClaim = context.User.FindFirst("company_id")?.Value;
            if (companyIdClaim != null && long.TryParse(companyIdClaim, out var companyId)) tenantContext.SetCompanyId(companyId);
            var membershipClaim = context.User.FindFirst("user_company_id")?.Value;
            if (membershipClaim != null && long.TryParse(membershipClaim, out var membershipId)) tenantContext.SetMembership(membershipId);
        }

        await _next(context);
    }
}
