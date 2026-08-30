namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.RecapWorkOrders;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.RecapWorkOrders;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using Xunit;

public class RecapWorkOrderServiceTests
{
    private readonly IRecapWorkOrderRepository _recapRepo = Substitute.For<IRecapWorkOrderRepository>();
    private readonly IBudgetPlanRepository _budgetPlanRepo = Substitute.For<IBudgetPlanRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IWamsMetrics _metrics = Substitute.For<IWamsMetrics>();
    private readonly IAuditLogWriter _auditLogWriter = Substitute.For<IAuditLogWriter>();
    private readonly RecapWorkOrderService _sut;

    public RecapWorkOrderServiceTests()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new RecapWorkOrderService(
            _recapRepo, _budgetPlanRepo, _userRepo, _rbacService, _warehouseContext, _metrics, _auditLogWriter);
    }

    private static RecapWorkOrder BuildPendingRecap(long id = 1, long budgetPlanId = 10, long companyId = 1, long warehouseId = 5)
        => new()
        {
            Id = id,
            BudgetPlanId = budgetPlanId,
            CompanyId = companyId,
            Status = RecapWorkOrderStatus.Pending,
            BudgetPlan = new BudgetPlan
            {
                Id = budgetPlanId,
                CompanyId = companyId,
                WarehouseShadowId = warehouseId,
                Items = [],
                WorkOrders = [],
            },
        };

    [Fact]
    public async Task RejectAsync_CascadesBudgetPlanToRejected_AndWritesAuditLogs()
    {
        var recap = BuildPendingRecap();
        _recapRepo.GetByIdWithDetailsAsync(recap.Id, Arg.Any<CancellationToken>()).Returns(recap);

        var projection = new RecapDetailProjection(
            recap.Id, recap.BudgetPlanId, recap.CompanyId, 5, "Rejected", "Reviewer", DateTime.UtcNow, "bad numbers",
            new RecapDetailHeader("BP-1", "TPL-1", "Rejected", null, DateTime.UtcNow, "WH1", "Warehouse 1", null),
            [], [], []);
        _recapRepo.GetDetailProjectionAsync(recap.Id, Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(projection);

        await _sut.RejectAsync(recap.Id, userId: 99, reviewerName: "Reviewer", reason: "bad numbers", ct: TestContext.Current.CancellationToken);

        await _budgetPlanRepo.Received(1).RejectViaRecapAsync(
            recap.BudgetPlanId, 99, Arg.Any<DateTime>(), "bad numbers", Arg.Any<CancellationToken>());

        await _auditLogWriter.Received(1).LogAsync(
            "UPDATE", "recap_work_orders", recap.Id, 99, Arg.Any<string?>(), Arg.Any<string?>(), recap.CompanyId,
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _auditLogWriter.Received(1).LogAsync(
            "UPDATE", "budget_plans", recap.BudgetPlanId, 99, Arg.Any<string?>(), Arg.Any<string?>(), recap.CompanyId,
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_ThrowsValidationException_WhenRecapNotPending()
    {
        var recap = BuildPendingRecap();
        recap.Status = RecapWorkOrderStatus.Approved;
        _recapRepo.GetByIdWithDetailsAsync(recap.Id, Arg.Any<CancellationToken>()).Returns(recap);

        var act = () => _sut.RejectAsync(recap.Id, userId: 99, reviewerName: "Reviewer", reason: "x");

        await act.Should().ThrowAsync<ValidationException>();
        await _budgetPlanRepo.DidNotReceive().RejectViaRecapAsync(
            Arg.Any<long>(), Arg.Any<long>(), Arg.Any<DateTime>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
