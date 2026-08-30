namespace WAMS.Api.Tests.Middleware;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using WAMS.Api.Middleware;
using WAMS.Application.Interfaces.Common;
using Xunit;

public class TenantMiddlewareTests
{
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly TenantMiddleware _sut = new(_ => Task.CompletedTask);

    private static HttpContext BuildContext(IEnumerable<Claim> claims, bool authenticated = true)
    {
        var ctx = new DefaultHttpContext();
        var identity = authenticated
            ? new ClaimsIdentity(claims, "Bearer")
            : new ClaimsIdentity();
        ctx.User = new ClaimsPrincipal(identity);
        return ctx;
    }

    [Fact]
    public async Task SuperAdminWithWildcardAndCompanyIdClaims_CallsSetCompanyId()
    {
        var ctx = BuildContext([
            new Claim("permissions", "*.*.*"),
            new Claim("company_id", "42")
        ]);

        await _sut.InvokeAsync(ctx, _tenantContext);

        _tenantContext.Received(1).SetCompanyId(42);
    }

    [Fact]
    public async Task RegularUserWithCompanyIdClaim_CallsSetCompanyId()
    {
        var ctx = BuildContext([new Claim("company_id", "42")]);

        await _sut.InvokeAsync(ctx, _tenantContext);

        _tenantContext.Received(1).SetCompanyId(42);
    }

    [Fact]
    public async Task UnauthenticatedRequest_DoesNotSetTenantContext()
    {
        var ctx = BuildContext([], authenticated: false);

        await _sut.InvokeAsync(ctx, _tenantContext);

        _tenantContext.DidNotReceive().SetCompanyId(Arg.Any<long>());
    }

    [Fact]
    public async Task AuthenticatedUserWithNoCompanyIdClaim_DoesNotSetTenantContext()
    {
        var ctx = BuildContext([]);

        await _sut.InvokeAsync(ctx, _tenantContext);

        _tenantContext.DidNotReceive().SetCompanyId(Arg.Any<long>());
    }

    [Fact]
    public async Task UnparsableCompanyIdClaim_DoesNotSetTenantContext()
    {
        var ctx = BuildContext([new Claim("company_id", "not-a-number")]);

        await _sut.InvokeAsync(ctx, _tenantContext);

        _tenantContext.DidNotReceive().SetCompanyId(Arg.Any<long>());
    }
}
