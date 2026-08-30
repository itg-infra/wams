namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.Warehouses;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.Warehouses;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Entities.Common;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Exceptions;
using Xunit;

public class WarehouseShadowServiceTests
{
    private readonly IWarehouseShadowRepository _warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly IProvinceRepository _provinceRepo = Substitute.For<IProvinceRepository>();
    private readonly WarehouseShadowService _sut;

    private static readonly WarehouseQuery DefaultQuery = new() { Page = 1, Limit = 20 };

    public WarehouseShadowServiceTests()
    {
        _sut = new WarehouseShadowService(_warehouseRepo, _userRepo, _rbacService, _provinceRepo);
    }

    // GetAllAsync - global access
    [Fact]
    public async Task GetAllAsync_WithGlobalAccess_QueriesAllWarehouses()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _warehouseRepo.GetAllAsync(Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<WarehouseShadow>(), 0));

        var result = await _sut.GetAllAsync(1, DefaultQuery, ct);

        result.Success.Should().BeTrue();
        await _warehouseRepo.Received(1).GetAllAsync(DefaultQuery, Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().GetUserWarehouseIdsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WithoutGlobalAccess_QueriesOnlyAssignedWarehouses()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, ct).Returns([7L, 8L]);
        _warehouseRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<WarehouseShadow>(), 0));

        var result = await _sut.GetAllAsync(1, DefaultQuery, ct);

        await _warehouseRepo.Received(1).GetByIdsAsync(
            Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 7, 8 })),
            DefaultQuery,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WithProvinceFilter_PassesLocationToRepo()
    {
        var ct = TestContext.Current.CancellationToken;
        var locationQuery = new WarehouseQuery { Page = 1, Limit = 20, ProvinceId = 1 };
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _warehouseRepo.GetAllAsync(Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<WarehouseShadow>(), 0));

        await _sut.GetAllAsync(1, locationQuery, ct);

        await _warehouseRepo.Received(1).GetAllAsync(locationQuery, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAllAsync_WithProvince_ReturnsProvinceFields()
    {
        var wh = TestBuilders.WarehouseShadow(id: 7, location: "Lampung", provinceId: 1);
        wh.Province = new Province { Id = 1, Name = "LAMPUNG", Display = "Lampung" };
        var ct = TestContext.Current.CancellationToken;
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _warehouseRepo.GetAllAsync(Arg.Any<WarehouseQuery>(), Arg.Any<CancellationToken>())
            .Returns((new List<WarehouseShadow> { wh }, 1));

        var result = await _sut.GetAllAsync(1, DefaultQuery, ct);

        result.Data.Should().ContainSingle();
        var item = result.Data.Single();
        item.ProvinceId.Should().Be(1);
        item.ProvinceName.Should().Be("LAMPUNG");
        item.ProvinceDisplay.Should().Be("Lampung");
    }

    // GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_WithWarehouseNotFound_ThrowsNotFoundException()
    {
        var ct = TestContext.Current.CancellationToken;
        _warehouseRepo.GetByIdAsync(99, ct).ReturnsNull();

        var act = () => _sut.GetByIdAsync(99, userId: 1, ct: ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithNoAccess_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        var wh = TestBuilders.WarehouseShadow(id: 7);
        _warehouseRepo.GetByIdAsync(7, ct).Returns(wh);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, ct).Returns(new List<long>());

        var act = () => _sut.GetByIdAsync(7, userId: 1, ct: ct);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithGlobalAccess_ReturnsResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var wh = TestBuilders.WarehouseShadow(id: 7);
        _warehouseRepo.GetByIdAsync(7, ct).Returns(wh);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.GetByIdAsync(7, userId: 1, ct: ct);

        result.Id.Should().Be(7);
        result.Code.Should().Be("WH-01");
    }

    [Fact]
    public async Task GetByIdAsync_WithAssignedWarehouse_ReturnsResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        var wh = TestBuilders.WarehouseShadow(id: 7);
        _warehouseRepo.GetByIdAsync(7, ct).Returns(wh);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, ct).Returns([7L]);

        var result = await _sut.GetByIdAsync(7, userId: 1, ct: ct);

        result.Id.Should().Be(7);
    }

    [Fact]
    public async Task GetByIdAsync_WithProvince_ReturnsProvinceFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var wh = TestBuilders.WarehouseShadow(id: 7, location: "Lampung", provinceId: 1);
        wh.Province = new Province { Id = 1, Name = "LAMPUNG", Display = "Lampung" };
        _warehouseRepo.GetByIdAsync(7, ct).Returns(wh);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.GetByIdAsync(7, userId: 1, ct: ct);

        result.ProvinceId.Should().Be(1);
        result.ProvinceName.Should().Be("LAMPUNG");
        result.ProvinceDisplay.Should().Be("Lampung");
    }

    [Fact]
    public async Task GetByIdAsync_WithoutProvince_ReturnsNullProvinceFields()
    {
        var ct = TestContext.Current.CancellationToken;
        var wh = TestBuilders.WarehouseShadow(id: 7);
        _warehouseRepo.GetByIdAsync(7, ct).Returns(wh);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.GetByIdAsync(7, userId: 1, ct: ct);

        result.ProvinceId.Should().BeNull();
        result.ProvinceName.Should().BeNull();
        result.ProvinceDisplay.Should().BeNull();
    }

    // GetDistinctLocationsAsync
    [Fact]
    public async Task GetDistinctLocationsAsync_WithGlobalAccess_ReturnsAllProvinces()
    {
        var ct = TestContext.Current.CancellationToken;
        var allProvinces = new List<(long Id, string Name, string Display)> { (1L, "LAMPUNG", "Lampung"), (2L, "SURABAYA", "Surabaya") };
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _provinceRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(allProvinces);

        var result = await _sut.GetDistinctLocationsAsync(1, ct);

        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(new ProvinceOption(1L, "LAMPUNG", "Lampung"));
        result.Should().ContainEquivalentOf(new ProvinceOption(2L, "SURABAYA", "Surabaya"));
        await _provinceRepo.Received(1).GetAllActiveAsync(Arg.Any<CancellationToken>());
        await _userRepo.DidNotReceive().GetUserWarehouseIdsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDistinctLocationsAsync_WithoutGlobalAccess_ReturnsOnlyUserProvinces()
    {
        var ct = TestContext.Current.CancellationToken;
        var allProvinces = new List<(long Id, string Name, string Display)> { (1L, "LAMPUNG", "Lampung"), (2L, "SURABAYA", "Surabaya"), (3L, "JAKARTA", "Jakarta") };
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _provinceRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(allProvinces);
        _userRepo.GetUserWarehouseIdsAsync(1, Arg.Any<CancellationToken>()).Returns([7L, 9L]);
        _warehouseRepo.GetProvinceIdsForWarehousesAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([1L, 3L]);

        var result = await _sut.GetDistinctLocationsAsync(1, ct);

        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(new ProvinceOption(1L, "LAMPUNG", "Lampung"));
        result.Should().ContainEquivalentOf(new ProvinceOption(3L, "JAKARTA", "Jakarta"));
        result.Should().NotContain(p => p.Id == 2L);
    }

    [Fact]
    public async Task GetDistinctLocationsAsync_WarehousePinOnly_IncludesThatWarehousesProvince()
    {
        // Regression guard: a user pinned to one warehouse (no direct province scope) must still
        // see that warehouse's province in the filter dropdown - it agrees with GetAllAsync's
        // warehouse-id-based access, not GetUserProvinceIdsAsync's direct-scope-only semantics.
        var ct = TestContext.Current.CancellationToken;
        var allProvinces = new List<(long Id, string Name, string Display)> { (1L, "LAMPUNG", "Lampung") };
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);
        _provinceRepo.GetAllActiveAsync(Arg.Any<CancellationToken>()).Returns(allProvinces);
        _userRepo.GetUserWarehouseIdsAsync(1, Arg.Any<CancellationToken>()).Returns([7L]);
        _warehouseRepo.GetProvinceIdsForWarehousesAsync(
            Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 7L })), Arg.Any<CancellationToken>())
            .Returns([1L]);

        var result = await _sut.GetDistinctLocationsAsync(1, ct);

        result.Should().ContainSingle().Which.Id.Should().Be(1L);
        await _userRepo.DidNotReceive().GetUserProvinceIdsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    // GetUnmappedAsync
    [Fact]
    public async Task GetUnmappedAsync_WithGlobalAccess_ReturnsUnmappedWarehouses()
    {
        var unmapped = new List<WarehouseShadow>
        {
            TestBuilders.WarehouseShadow(id: 1, code: "WH-01", location: "Lampung"),
            TestBuilders.WarehouseShadow(id: 2, code: "WH-02", location: "Surabaya"),
        };
        var ct = TestContext.Current.CancellationToken;
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _warehouseRepo.GetUnmappedAsync(Arg.Any<CancellationToken>()).Returns(unmapped);

        var result = await _sut.GetUnmappedAsync(userId: 1, ct: ct);

        result.Should().HaveCount(2);
        result.Select(r => r.Code).Should().BeEquivalentTo(["WH-01", "WH-02"]);
        result.Should().AllSatisfy(r =>
        {
            r.ProvinceId.Should().BeNull();
            r.ProvinceName.Should().BeNull();
            r.ProvinceDisplay.Should().BeNull();
        });
        await _warehouseRepo.Received(1).GetUnmappedAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnmappedAsync_WithoutGlobalAccess_ThrowsForbiddenException()
    {
        var ct = TestContext.Current.CancellationToken;
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.GetUnmappedAsync(userId: 1, ct: ct);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _warehouseRepo.DidNotReceive().GetUnmappedAsync(Arg.Any<CancellationToken>());
    }
}
