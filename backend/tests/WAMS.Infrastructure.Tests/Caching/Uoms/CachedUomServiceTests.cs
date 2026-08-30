namespace WAMS.Infrastructure.Tests.Caching;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Uoms;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Infrastructure.Caching.Uoms;
using Xunit;

public sealed class CachedUomServiceTests : IDisposable
{
    private readonly CacheTestFixture _fx = new();
    private readonly IUomService _inner = Substitute.For<IUomService>();
    private readonly CachedUomService _sut;

    private static readonly UomResponse Uom1 = new(1, "KG", "Kilogram", true);
    private static readonly UomResponse Uom1Updated = new(1, "KG", "Kilogram Updated", true);

    public CachedUomServiceTests()
    {
        _sut = new CachedUomService(_inner, _fx.Cache, _fx.Options);
    }

    public void Dispose() => _fx.Dispose();

    [Fact]
    public async Task GetAllAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetAllAsync(true, Arg.Any<CancellationToken>()).Returns([Uom1]);

        await _sut.GetAllAsync(true, TestContext.Current.CancellationToken);
        await _sut.GetAllAsync(true, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetAllAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_CachesResult_InnerCalledOnce()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1);

        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _inner.Received(1).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1);
        _inner.CreateAsync(Arg.Any<CreateUomRequest>(), Arg.Any<CancellationToken>()).Returns(Uom1);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.CreateAsync(new CreateUomRequest("PCS", "Pieces"), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1Updated);
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Kilogram Updated", "cache cleared after Create");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1);
        _inner.UpdateAsync(1, Arg.Any<UpdateUomRequest>(), Arg.Any<CancellationToken>()).Returns(Uom1Updated);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.UpdateAsync(1, new UpdateUomRequest("Kilogram Updated", true), TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1Updated);
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Kilogram Updated", "cache cleared after Update");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesCache_NextReadHitsInner()
    {
        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1);
        await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);

        await _sut.DeleteAsync(1, TestContext.Current.CancellationToken);

        _inner.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Uom1Updated);
        var result = await _sut.GetByIdAsync(1, TestContext.Current.CancellationToken);
        result.Name.Should().Be("Kilogram Updated", "cache cleared after Delete");
        await _inner.Received(2).GetByIdAsync(1, Arg.Any<CancellationToken>());
    }
}
