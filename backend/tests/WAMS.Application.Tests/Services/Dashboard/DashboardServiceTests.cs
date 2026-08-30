namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.Dashboard;
using WAMS.Application.Interfaces.Dashboard;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.Dashboard;
using Xunit;

public class DashboardServiceTests
{
    private readonly IDashboardRepository _repo = Substitute.For<IDashboardRepository>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _sut = new DashboardService(_repo, _warehouseContext, _userRepo, _rbacService);
    }

    // --- GetSummaryAsync ---

    [Fact]
    public async Task GetSummaryAsync_WithWarehouseHeader_PassesSingleWarehouseId()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);
        var expected = BuildSummary();
        _repo.GetSummaryAsync(
                Arg.Is<IReadOnlyList<long>?>(ids => ids != null && ids.SequenceEqual(new long[] { 7L })),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetSummaryAsync(1, ["WAREHOUSE_ADMIN"], CancellationToken.None);

        result.Should().Be(expected);
        await _rbacService.DidNotReceive().HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSummaryAsync_WithGlobalAccess_PassesNullWarehouseIds()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var expected = BuildSummary();
        _repo.GetSummaryAsync(
                Arg.Is<IReadOnlyList<long>?>(ids => ids == null),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetSummaryAsync(1, ["HO_SPV"], CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetSummaryAsync_WithoutGlobalAccess_PassesUserWarehouseIds()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1, Arg.Any<CancellationToken>()).Returns([3L, 5L]);
        var expected = BuildSummary();
        _repo.GetSummaryAsync(
                Arg.Is<IReadOnlyList<long>?>(ids => ids != null && ids.SequenceEqual(new long[] { 3L, 5L })),
                Arg.Any<IReadOnlyList<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.GetSummaryAsync(1, ["WAREHOUSE_ADMIN"], CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetSummaryAsync_ForwardsRoleNamesToRepository()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetSummaryAsync(Arg.Any<IReadOnlyList<long>?>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(BuildSummary());

        await _sut.GetSummaryAsync(1, ["ROLE_A", "ROLE_B"], CancellationToken.None);

        await _repo.Received(1).GetSummaryAsync(
            Arg.Any<IReadOnlyList<long>?>(),
            Arg.Is<IReadOnlyList<string>>(r => r.SequenceEqual(new[] { "ROLE_A", "ROLE_B" })),
            Arg.Any<CancellationToken>());
    }

    // --- GetTodayActivitiesAsync ---

    [Fact]
    public async Task GetTodayActivitiesAsync_WithWarehouseHeader_ScopesToSingleWarehouse()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(9L);
        _repo.GetTodayActivitiesAsync(
                Arg.Any<DashboardActivityQuery>(),
                Arg.Is<IReadOnlyList<long>?>(ids => ids != null && ids.SequenceEqual(new long[] { 9L })),
                Arg.Any<CancellationToken>())
            .Returns((new List<DashboardActivityResponse> { BuildActivity() }, 1));

        var (items, total) = await _sut.GetTodayActivitiesAsync(new DashboardActivityQuery(), 1, CancellationToken.None);

        items.Should().HaveCount(1);
        total.Should().Be(1);
    }

    [Fact]
    public async Task GetTodayActivitiesAsync_WithoutGlobalAccess_ScopesToUserWarehouses()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(2, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(2, Arg.Any<CancellationToken>()).Returns([1L, 2L]);
        _repo.GetTodayActivitiesAsync(
                Arg.Any<DashboardActivityQuery>(),
                Arg.Is<IReadOnlyList<long>?>(ids => ids != null && ids.SequenceEqual(new long[] { 1L, 2L })),
                Arg.Any<CancellationToken>())
            .Returns((new List<DashboardActivityResponse>(), 0));

        var (items, total) = await _sut.GetTodayActivitiesAsync(new DashboardActivityQuery(), 2, CancellationToken.None);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    // --- GetHistoryAsync ---

    [Fact]
    public async Task GetHistoryAsync_WithGlobalAccess_PassesNullWarehouseIds()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        var expected = new DashboardHistoryResponse([], []);
        _repo.GetHistoryAsync(2026, 6, null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetHistoryAsync(2026, 6, 1, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetHistoryAsync_ForwardsYearAndMonthToRepository()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1, Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetHistoryAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<long>?>(), Arg.Any<CancellationToken>())
            .Returns(new DashboardHistoryResponse([], []));

        await _sut.GetHistoryAsync(2025, 3, 1, CancellationToken.None);

        await _repo.Received(1).GetHistoryAsync(2025, 3, Arg.Any<IReadOnlyList<long>?>(), Arg.Any<CancellationToken>());
    }

    private static DashboardSummaryResponse BuildSummary() =>
        new(88m, 1_170_000_000m, 1_029_600_000m, 14, 3, 42, 6, 8, 2);

    private static DashboardActivityResponse BuildActivity() =>
        new(1, "2603000001", "PT. XYZ, PT. ABC", "Bongkaran", true, "Lampung", DateTime.UtcNow, "Approved", "Approved");
}
