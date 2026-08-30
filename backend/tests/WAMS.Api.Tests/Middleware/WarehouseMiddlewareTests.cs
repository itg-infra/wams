namespace WAMS.Api.Tests.Middleware;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using WAMS.Api.Middleware;
using WAMS.Application.Interfaces.Warehouses;
using Xunit;

public class WarehouseMiddlewareTests
{
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly WarehouseMiddleware _sut = new(_ => Task.CompletedTask);

    private static HttpContext BuildContext(
        IEnumerable<Claim> claims, string? warehouseHeader = null, bool authenticated = true)
    {
        var ctx = new DefaultHttpContext();
        var identity = authenticated
            ? new ClaimsIdentity(claims, "Bearer")
            : new ClaimsIdentity();
        ctx.User = new ClaimsPrincipal(identity);

        if (warehouseHeader is not null)
            ctx.Request.Headers["X-Warehouse-Id"] = warehouseHeader;

        return ctx;
    }

    [Fact]
    public async Task SuperAdminWithWarehouseHeader_CallsSetWarehouseId_NotBypassMode()
    {
        var ctx = BuildContext([new Claim("permissions", "*.*.*")], warehouseHeader: "7");

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.Received(1).SetWarehouseId(7);
        _warehouseContext.DidNotReceive().SetBypassMode();
    }

    [Fact]
    public async Task SuperAdminWithoutWarehouseHeader_CallsSetBypassMode()
    {
        var ctx = BuildContext([new Claim("permissions", "*.*.*")]);

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.Received(1).SetBypassMode();
        _warehouseContext.DidNotReceive().SetWarehouseId(Arg.Any<long>());
    }

    [Fact]
    public async Task SuperAdminWithUnparsableWarehouseHeader_CallsSetBypassMode()
    {
        var ctx = BuildContext([new Claim("permissions", "*.*.*")], warehouseHeader: "not-a-number");

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.Received(1).SetBypassMode();
        _warehouseContext.DidNotReceive().SetWarehouseId(Arg.Any<long>());
    }

    [Fact]
    public async Task RegularUserWithWarehouseHeader_CallsSetWarehouseId()
    {
        var ctx = BuildContext([], warehouseHeader: "3");

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.Received(1).SetWarehouseId(3);
        _warehouseContext.DidNotReceive().SetBypassMode();
    }

    [Fact]
    public async Task RegularUserWithoutWarehouseHeader_DoesNotSetWarehouseContext()
    {
        var ctx = BuildContext([]);

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.DidNotReceive().SetWarehouseId(Arg.Any<long>());
        _warehouseContext.DidNotReceive().SetBypassMode();
    }

    [Fact]
    public async Task RegularUserWithUnparsableWarehouseHeader_DoesNotSetWarehouseContext()
    {
        var ctx = BuildContext([], warehouseHeader: "not-a-number");

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.DidNotReceive().SetWarehouseId(Arg.Any<long>());
        _warehouseContext.DidNotReceive().SetBypassMode();
    }

    [Fact]
    public async Task UnauthenticatedRequest_DoesNotSetWarehouseContext()
    {
        var ctx = BuildContext([], warehouseHeader: "3", authenticated: false);

        await _sut.InvokeAsync(ctx, _warehouseContext);

        _warehouseContext.DidNotReceive().SetWarehouseId(Arg.Any<long>());
        _warehouseContext.DidNotReceive().SetBypassMode();
    }
}
