namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Common;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Infrastructure.Caching.Warehouses;
using Xunit;

public sealed class CachedWarehouseShadowServiceTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IWarehouseShadowService _inner = Substitute.For<IWarehouseShadowService>();
    private readonly CachedWarehouseShadowService _sut;

    private static readonly WarehouseResponse Wh1 = new(1, "WH01", "Main Warehouse", "Jakarta", true, DateTime.UtcNow, DateTime.UtcNow);
    private static readonly WarehouseQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    public CachedWarehouseShadowServiceTests()
    {
        _sut = new CachedWarehouseShadowService(_inner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task GetByIdAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetByIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(Wh1);

        await _sut.GetByIdAsync(1, userId: 99, TestContext.Current.CancellationToken);
        await _sut.GetByIdAsync(1, userId: 99, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetByIdAsync(1, 99, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_DifferentUsers_CachedSeparately()
    {
        var forUser1 = Wh1 with { Name = "For User 1" };
        var forUser2 = Wh1 with { Name = "For User 2" };
        _inner.GetByIdAsync(1, 1, Arg.Any<CancellationToken>()).Returns(forUser1);
        _inner.GetByIdAsync(1, 2, Arg.Any<CancellationToken>()).Returns(forUser2);

        var result1 = await _sut.GetByIdAsync(1, userId: 1, TestContext.Current.CancellationToken);
        var result2 = await _sut.GetByIdAsync(1, userId: 2, TestContext.Current.CancellationToken);

        result1.Name.Should().Be("For User 1");
        result2.Name.Should().Be("For User 2");
    }

    [Fact]
    public async Task GetDistinctLocationsAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetDistinctLocationsAsync(99, Arg.Any<CancellationToken>()).Returns(
            new List<ProvinceOption> { new(1L, "JAKARTA", "Jakarta"), new(2L, "SURABAYA", "Surabaya") });

        await _sut.GetDistinctLocationsAsync(userId: 99, TestContext.Current.CancellationToken);
        await _sut.GetDistinctLocationsAsync(userId: 99, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetDistinctLocationsAsync(99, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetAllAsync(99, DefaultQuery, Arg.Any<CancellationToken>())
            .Returns(new PaginatedResponse<WarehouseResponse>(true, [Wh1], new PaginationMeta(1, 20, 1, 1)));

        await _sut.GetAllAsync(userId: 99, DefaultQuery, TestContext.Current.CancellationToken);
        await _sut.GetAllAsync(userId: 99, DefaultQuery, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetAllAsync(99, DefaultQuery, Arg.Any<CancellationToken>());
    }
}
