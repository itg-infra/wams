namespace WAMS.Api.Tests.Filters;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using WAMS.Api.Filters;
using WAMS.Application.Interfaces.Auth;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Domain.Exceptions;
using Xunit;

public class RequirePermissionAttributeTests
{
    private readonly ITokenService _tokenSvc = Substitute.For<ITokenService>();
    private readonly IRbacService _rbacSvc = Substitute.For<IRbacService>();

    private AuthorizationFilterContext BuildContext(ClaimsPrincipal? user = null, bool isAuthenticated = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_tokenSvc);
        services.AddSingleton(_rbacSvc);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = user ?? BuildAuthenticatedUser(userId: 1, jti: "test-jti"),
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }

    private static ClaimsPrincipal BuildAuthenticatedUser(long userId = 1, string jti = "jti-abc")
    {
        var identity = new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
        ], authenticationType: "jwt");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal AnonymousUser()
        => new ClaimsPrincipal(new ClaimsIdentity());

    private static ClaimsPrincipal UserWithoutSub(string jti = "jti-abc")
    {
        var identity = new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Jti, jti),
        ], authenticationType: "jwt");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        var attr = new RequirePermissionAttribute("user.user.read");
        var ctx = BuildContext(user: AnonymousUser());

        var act = async () => await attr.OnAuthorizationAsync(ctx);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Authentication required");
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenJtiIsBlacklisted_ThrowsUnauthorized()
    {
        _tokenSvc.IsTokenBlacklistedAsync("test-jti").Returns(true);
        var attr = new RequirePermissionAttribute("user.user.read");
        var ctx = BuildContext();

        var act = async () => await attr.OnAuthorizationAsync(ctx);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Token has been revoked");
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithMissingSubClaim_ThrowsUnauthorized()
    {
        _tokenSvc.IsTokenBlacklistedAsync(Arg.Any<string>()).Returns(false);
        var attr = new RequirePermissionAttribute("user.user.read");
        var ctx = BuildContext(user: UserWithoutSub());

        var act = async () => await attr.OnAuthorizationAsync(ctx);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid token: missing or invalid subject claim");
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenUserLacksPermission_ThrowsForbidden()
    {
        _tokenSvc.IsTokenBlacklistedAsync("test-jti").Returns(false);
        _rbacSvc.HasPermissionAsync(1, "user", "user", "read", Arg.Any<CancellationToken>()).Returns(false);
        var attr = new RequirePermissionAttribute("user.user.read");
        var ctx = BuildContext();

        var act = async () => await attr.OnAuthorizationAsync(ctx);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Missing permission: user.user.read");
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenPermissionGranted_CompletesWithoutException()
    {
        _tokenSvc.IsTokenBlacklistedAsync("test-jti").Returns(false);
        _rbacSvc.HasPermissionAsync(1, "user", "user", "read", Arg.Any<CancellationToken>()).Returns(true);
        var attr = new RequirePermissionAttribute("user.user.read");
        var ctx = BuildContext();

        var act = async () => await attr.OnAuthorizationAsync(ctx);

        await act.Should().NotThrowAsync();
        ctx.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenNoJtiClaim_SkipsBlacklistAndChecksRbac()
    {
        var identity = new ClaimsIdentity([
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
        ], authenticationType: "jwt");
        var user = new ClaimsPrincipal(identity);

        _rbacSvc.HasPermissionAsync(1, "stock", "item", "read", Arg.Any<CancellationToken>()).Returns(true);
        var attr = new RequirePermissionAttribute("stock.item.read");
        var ctx = BuildContext(user: user);

        await attr.OnAuthorizationAsync(ctx);

        await _tokenSvc.DidNotReceive().IsTokenBlacklistedAsync(Arg.Any<string>());
        ctx.Result.Should().BeNull();
    }
}
