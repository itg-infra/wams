namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.Common;
using WAMS.Application.DTOs.FinanceReports;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Interfaces.FinanceReports;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.FinanceReports;
using WAMS.Domain.Exceptions;
using Xunit;

public class FinanceReportServiceTests
{
    private const long UserId = 1L;

    private readonly IFinanceReportRepository repo = Substitute.For<IFinanceReportRepository>();
    private readonly IPurchaseOrderService poService = Substitute.For<IPurchaseOrderService>();
    private readonly IWarehouseContext warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IUserRepository userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService rbacService = Substitute.For<IRbacService>();
    private readonly IWarehouseShadowRepository warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly FinanceReportService sut;

    public FinanceReportServiceTests()
    {
        sut = new FinanceReportService(repo, poService, warehouseContext, userRepo, rbacService, warehouseRepo);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToPurchaseOrderService()
    {
        var query = new DataTableQuery { Page = 1, Limit = 10 };
        var expected = (new List<ApprovedBudgetPlanPoStatusResponse>(), 0);
        poService.GetApprovedBudgetPlansAsync(UserId, query, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await sut.GetAllAsync(query, UserId, CancellationToken.None);

        result.Should().Be(expected);
        await poService.Received(1).GetApprovedBudgetPlansAsync(UserId, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDetailAsync_WarehouseScoped_UserHasGlobalAccess_PassesSingleWarehouseId()
    {
        warehouseContext.IsSet.Returns(true);
        warehouseContext.WarehouseId.Returns(42L);
        warehouseRepo.GetByIdAsync(42L, Arg.Any<CancellationToken>()).Returns(new WAMS.Domain.Entities.Warehouses.WarehouseShadow { Id = 42L });
        rbacService.HasGlobalAccessAsync(UserId, Arg.Any<CancellationToken>()).Returns(true);
        var expected = MakeDetail();
        repo.GetDetailAsync(5L, Arg.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 42L), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await sut.GetDetailAsync(5L, UserId, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetDetailAsync_WarehouseScoped_UserAssignedToWarehouse_PassesSingleWarehouseId()
    {
        warehouseContext.IsSet.Returns(true);
        warehouseContext.WarehouseId.Returns(42L);
        warehouseRepo.GetByIdAsync(42L, Arg.Any<CancellationToken>()).Returns(new WAMS.Domain.Entities.Warehouses.WarehouseShadow { Id = 42L });
        rbacService.HasGlobalAccessAsync(UserId, Arg.Any<CancellationToken>()).Returns(false);
        userRepo.GetUserWarehouseIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([42L]);
        var expected = MakeDetail();
        repo.GetDetailAsync(5L, Arg.Is<IReadOnlyList<long>>(ids => ids.Count == 1 && ids[0] == 42L), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await sut.GetDetailAsync(5L, UserId, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetDetailAsync_WarehouseScoped_UserNotAssignedAndNoGlobalAccess_ThrowsForbiddenBeforeQuery()
    {
        warehouseContext.IsSet.Returns(true);
        warehouseContext.WarehouseId.Returns(2L);
        warehouseRepo.GetByIdAsync(2L, Arg.Any<CancellationToken>()).Returns(new WAMS.Domain.Entities.Warehouses.WarehouseShadow { Id = 2L });
        rbacService.HasGlobalAccessAsync(UserId, Arg.Any<CancellationToken>()).Returns(false);
        userRepo.GetUserWarehouseIdsAsync(UserId, Arg.Any<CancellationToken>()).Returns([1L]);

        var act = () => sut.GetDetailAsync(5L, UserId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await repo.DidNotReceive().GetDetailAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<long>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDetailAsync_WarehouseScoped_WarehouseDoesNotExist_ThrowsNotFoundBeforeQuery()
    {
        warehouseContext.IsSet.Returns(true);
        warehouseContext.WarehouseId.Returns(999L);
        warehouseRepo.GetByIdAsync(999L, Arg.Any<CancellationToken>()).Returns((WAMS.Domain.Entities.Warehouses.WarehouseShadow?)null);

        var act = () => sut.GetDetailAsync(5L, UserId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await repo.DidNotReceive().GetDetailAsync(Arg.Any<long>(), Arg.Any<IReadOnlyList<long>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDetailAsync_NoHeaderMatch_ThrowsNotFoundException()
    {
        warehouseContext.IsSet.Returns(false);
        rbacService.HasGlobalAccessAsync(UserId, Arg.Any<CancellationToken>()).Returns(true);
        repo.GetDetailAsync(99L, null, Arg.Any<CancellationToken>()).Returns((FinanceReportDetailResponse?)null);

        var act = () => sut.GetDetailAsync(99L, UserId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static FinanceReportDetailResponse MakeDetail() => new(
        Header: new FinanceReportHeaderResponse(1, "BP.001", "T.0001", "Draft", null, DateTime.UtcNow, "WH01", "Warehouse 1", "Lampung"),
        CostDetails: [],
        Dpp: 0, TotalPpn: 0, TotalPph: 0, GrandTotal: 0,
        BudgetRecap: new FinanceReportBudgetRecapResponse(0, 0, 0));
}
