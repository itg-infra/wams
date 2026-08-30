namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.Notifications;
using WAMS.Application.Interfaces.ActivityTypes;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.BudgetTemplates;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Items;
using WAMS.Application.Interfaces.Notifications;
using WAMS.Application.Interfaces.RateCards;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.Spk;
using WAMS.Application.Interfaces.Uoms;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Application.Interfaces.WorkflowTemplates;
using WAMS.Application.Services.BudgetPlans;
using WAMS.Application.Tests.Helpers;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.RateCards;
using WAMS.Domain.Entities.Spk;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Entities.WorkflowTemplates;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using Xunit;

public class BudgetPlanServiceTests
{
    private readonly IBudgetPlanRepository _budgetPlanRepo = Substitute.For<IBudgetPlanRepository>();
    private readonly IBudgetTemplateRepository _budgetTemplateRepo = Substitute.For<IBudgetTemplateRepository>();
    private readonly IWarehouseShadowRepository _warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly IRateCardRepository _rateCardRepo = Substitute.For<IRateCardRepository>();
    private readonly IVendorShadowRepository _vendorRepo = Substitute.For<IVendorShadowRepository>();
    private readonly ISpkShadowRepository _spkRepo = Substitute.For<ISpkShadowRepository>();
    private readonly IItemShadowRepository _itemShadowRepo = Substitute.For<IItemShadowRepository>();
    private readonly ICodeCounterRepository _codeCounterRepo = Substitute.For<ICodeCounterRepository>();
    private readonly IUomMasterRepository _uomRepo = Substitute.For<IUomMasterRepository>();
    private readonly IActivityTypeRepository _activityTypeRepo = Substitute.For<IActivityTypeRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IWorkflowRepository _workflowRepo = Substitute.For<IWorkflowRepository>();
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly IWamsMetrics _metrics = Substitute.For<IWamsMetrics>();
    private readonly IWorkOrderService _woService = Substitute.For<IWorkOrderService>();
    private readonly IRecapWorkOrderRepository _recapRepo = Substitute.For<IRecapWorkOrderRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IAuditLogWriter _auditLogWriter = Substitute.For<IAuditLogWriter>();
    private readonly BudgetPlanService _sut;

    public BudgetPlanServiceTests()
    {
        _tenantContext.CompanyId.Returns(1L);
        // Default stub: any activity type id requested resolves to a found, active ActivityType.
        // Individual tests that need to exercise NotFound/NotActive can override this.
        _activityTypeRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ((IEnumerable<long>)ci[0])
                .Distinct()
                .Select(id => new ActivityType { Id = id, Code = $"AT{id}", Name = $"Activity {id}", IsActive = true })
                .ToList());
        _sut = new BudgetPlanService(
            _budgetPlanRepo,
            _budgetTemplateRepo,
            _warehouseRepo,
            _rateCardRepo,
            _vendorRepo,
            _spkRepo,
            _itemShadowRepo,
            _codeCounterRepo,
            _uomRepo,
            _activityTypeRepo,
            _uow,
            _warehouseContext,
            _userRepo,
            _workflowRepo,
            _notificationService,
            _rbacService,
            _metrics,
            _woService,
            _recapRepo,
            _tenantContext,
            _auditLogWriter,
            NullLogger<BudgetPlanService>.Instance);
    }

    [Fact]
    public async Task ApproveAsync_NonFinalStage_SetsInApprovalAndPublishesNotifications()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(50, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _userRepo.GetUsersByRolesAndWarehouseAsync(1, 10, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([TestBuilders.ActiveUser(id: 70)]);

        var ct = TestContext.Current.CancellationToken;
        await _sut.ApproveAsync(1, 50, ["WAREHOUSE_HEAD"], ct);

        plan.Status.Should().Be(BudgetPlanStatus.InApproval);
        await _notificationService.Received(1).PublishAsync(
            Arg.Is<IEnumerable<NotificationCreateRequest>>(items =>
                items.Count() == 2
                && items.Any(x => x.RecipientUserId == 99 && x.Type == "budget_plan_stage_approved")
                && items.Any(x => x.RecipientUserId == 70 && x.Type == "budget_plan_pending_approval")),
            Arg.Any<CancellationToken>());
    }

    // Segregation of duties: self-approval requires the approval.self.approve permission,
    // which the budget.*.* wildcard (HO_SPV, LOG_MGR, LOG_SPV) does not confer.
    [Fact]
    public async Task ApproveAsync_SelfApprovalWithoutPermission_Throws()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(88, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _rbacService.HasPermissionAsync(88, "approval", "self", "approve", Arg.Any<CancellationToken>())
            .Returns(false);

        var ct = TestContext.Current.CancellationToken;

        // 88 is the plan's SubmittedByUserId
        var act = () => _sut.ApproveAsync(1, 88, ["WAREHOUSE_HEAD"], ct);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        plan.Status.Should().Be(BudgetPlanStatus.Submitted);
    }

    [Fact]
    public async Task ApproveAsync_SelfApprovalWithPermission_Succeeds()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(88, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _userRepo.GetUsersByRolesAndWarehouseAsync(1, 10, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([TestBuilders.ActiveUser(id: 70)]);
        _rbacService.HasPermissionAsync(88, "approval", "self", "approve", Arg.Any<CancellationToken>())
            .Returns(true);

        var ct = TestContext.Current.CancellationToken;
        await _sut.ApproveAsync(1, 88, ["WAREHOUSE_HEAD"], ct);

        plan.Status.Should().Be(BudgetPlanStatus.InApproval);
    }

    [Fact]
    public async Task ApproveAsync_ApproverIsNotSubmitter_DoesNotCheckSelfApprovePermission()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(50, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _userRepo.GetUsersByRolesAndWarehouseAsync(1, 10, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([TestBuilders.ActiveUser(id: 70)]);

        var ct = TestContext.Current.CancellationToken;
        await _sut.ApproveAsync(1, 50, ["WAREHOUSE_HEAD"], ct);

        await _rbacService.DidNotReceive().HasPermissionAsync(
            Arg.Any<long>(), "approval", "self", "approve", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_FinalStage_SetsApprovedAndPublishesFinalNotification()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.InApproval, pendingStageOrder: 2);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(51, 10, Arg.Any<CancellationToken>()).Returns((true, true));

        var ct = TestContext.Current.CancellationToken;
        await _sut.ApproveAsync(1, 51, ["COORDINATOR_WH"], ct);

        plan.Status.Should().Be(BudgetPlanStatus.Approved);
        await _notificationService.Received(1).PublishAsync(
            Arg.Is<IEnumerable<NotificationCreateRequest>>(items =>
                items.Single().RecipientUserId == 99 &&
                items.Single().Type == "budget_plan_approved_final"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_FinalStage_CallsBulkCreateDraftAsync()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.InApproval, pendingStageOrder: 2);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(51, 10, Arg.Any<CancellationToken>()).Returns((true, true));

        var ct = TestContext.Current.CancellationToken;
        await _sut.ApproveAsync(1, 51, ["COORDINATOR_WH"], ct);

        await _woService.Received(1).BulkCreateDraftAsync(plan.Id, 51, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_NonFinalStage_DoesNotCallBulkCreateDraftAsync()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(50, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _userRepo.GetUsersByRolesAndWarehouseAsync(1, 10, Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([TestBuilders.ActiveUser(id: 70)]);

        var ct = TestContext.Current.CancellationToken;
        await _sut.ApproveAsync(1, 50, ["WAREHOUSE_HEAD"], ct);

        await _woService.DidNotReceive().BulkCreateDraftAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_PublishesRequesterNotification()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(52, 10, Arg.Any<CancellationToken>()).Returns((true, true));

        var ct = TestContext.Current.CancellationToken;
        await _sut.RejectAsync(1, 52, new RejectBudgetPlanRequest("No"), ct);

        plan.Status.Should().Be(BudgetPlanStatus.Rejected);
        await _notificationService.Received(1).PublishAsync(
            Arg.Is<IEnumerable<NotificationCreateRequest>>(items =>
                items.Single().RecipientUserId == 99 &&
                items.Single().Type == "budget_plan_rejected"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_SelfApproval_ThrowsValidationException()
    {
        var plan = BuildPlanWithWorkflow(BudgetPlanStatus.Submitted, pendingStageOrder: 1);
        _budgetPlanRepo.GetByIdForApprovalAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(88, 10, Arg.Any<CancellationToken>()).Returns((true, true));

        var act = () => _sut.ApproveAsync(1, 88, ["WAREHOUSE_HEAD"]);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*approve a budget plan you submitted*");
    }

    [Fact]
    public async Task CreateAsync_ValidatesWarehouseMatchesTemplateLocation()
    {
        var template = new BudgetTemplate
        {
            Id = 5,
            CompanyId = 1,
            Status = BudgetTemplateStatus.Submitted,
            ProvinceId = 1,
            Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 20, location: "Surabaya", provinceId: 2);

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(warehouse);

        var request = new CreateBudgetPlanRequest(
            BudgetTemplateId: 5,
            WarehouseShadowId: 20,
            Remark: null,
            DocDate: DateTime.UtcNow,
            Items: [],
            SpkShadowIds: null);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*province*");
    }

    // Reproduces the cross-tenant leak: in Super Admin bypass mode the tenant query filter on
    // WarehouseShadow is disabled, so GetByIdAsync can return a warehouse from a different
    // company than the plan's own tenant. CreateAsync must reject that explicitly instead of
    // silently stamping the plan with the caller's company while pointing at another company's
    // warehouse.
    [Fact]
    public async Task CreateAsync_WarehouseBelongsToDifferentCompany_ThrowsNotFoundException()
    {
        var template = new BudgetTemplate
        {
            Id = 5,
            CompanyId = 1,
            Status = BudgetTemplateStatus.Submitted,
            ProvinceId = 1,
            Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 20, companyId: 2, location: "Jakarta", provinceId: 1);

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(warehouse);
        _tenantContext.CompanyId.Returns(1L);

        var request = new CreateBudgetPlanRequest(
            BudgetTemplateId: 5,
            WarehouseShadowId: 20,
            Remark: null,
            DocDate: DateTime.UtcNow,
            Items: [],
            SpkShadowIds: null);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().ThrowAsync<NotFoundException>();
        await _budgetPlanRepo.DidNotReceive().CreateAsync(Arg.Any<BudgetPlan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SetsWarehouseShadowId_WhenLocationMatches()
    {
        var template = new BudgetTemplate
        {
            Id = 5,
            CompanyId = 1,
            Status = BudgetTemplateStatus.Submitted,
            ProvinceId = 1,
            Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<VendorShadow>());
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long VendorShadowId, long ItemShadowId)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem>());
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        var request = new CreateBudgetPlanRequest(
            BudgetTemplateId: 5,
            WarehouseShadowId: 10,
            Remark: null,
            DocDate: DateTime.UtcNow,
            Items: [],
            SpkShadowIds: null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.CreateAsync(99, request, ct);

        await _budgetPlanRepo.Received(1).CreateAsync(
            Arg.Is<BudgetPlan>(p => p.WarehouseShadowId == 10 && p.Code.StartsWith("BP-")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_UsesDirectWarehouseShadowId_ForAccessCheck()
    {
        _budgetPlanRepo.GetWarehouseShadowIdAsync(1, Arg.Any<CancellationToken>()).Returns((long?)10L);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _budgetPlanRepo.GetByIdProjectionAsync(1, Arg.Any<CancellationToken>()).Returns(MinimalBudgetPlanResponse());

        var ct = TestContext.Current.CancellationToken;
        await _sut.GetByIdAsync(1, userId: 99, ct: ct);

        await _userRepo.Received(1).CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithVendorFilter_ReturnsOnlyItemsForThatVendor()
    {
        _budgetPlanRepo.GetWarehouseShadowIdAsync(1, Arg.Any<CancellationToken>()).Returns((long?)10L);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _budgetPlanRepo.GetByIdProjectionAsync(1, Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse() with
            {
                Items =
                [
                    BudgetPlanItem(11, vendorShadowId: 22),
                    BudgetPlanItem(12, vendorShadowId: 33),
                ]
            });

        var result = await _sut.GetByIdAsync(
            1,
            userId: 99,
            ct: TestContext.Current.CancellationToken,
            vendorShadowId: 22);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be(11);
        result.Items[0].VendorShadowId.Should().Be(22);
    }

    [Fact]
    public async Task CreateAsync_Item_UsesRateCardUom_WhenUomMasterIdNotProvided()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem { Id = 1, ItemShadowId = 11, UomMasterId = 55, CostValue = 100m };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 1, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: null,
            UomMasterId: null);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.CreateAsync(99, request, ct);

        await _budgetPlanRepo.Received(1).CreateAsync(
            Arg.Is<BudgetPlan>(p => p.Items.Single().UomMasterId == 55),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Item_UsesProvidedUomMasterId_WhenUomExists()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem { Id = 1, ItemShadowId = 11, UomMasterId = 55, CostValue = 100m };
        var overrideUom = new UomMaster { Id = 77, Code = "TON", Name = "Ton" };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.Contains(77L)), Arg.Any<CancellationToken>())
            .Returns([overrideUom]);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 1, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: null,
            UomMasterId: 77);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.CreateAsync(99, request, ct);

        await _budgetPlanRepo.Received(1).CreateAsync(
            Arg.Is<BudgetPlan>(p => p.Items.Single().UomMasterId == 77),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_Item_LinkedToSpkWithZeroQuantity_DoesNotThrowQuantityExceeded()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem { Id = 1, ItemShadowId = 11, UomMasterId = 55, CostValue = 100m };
        var spkWithZeroQty = new SpkShadow
        {
            Id = 30, CompanyId = 1, Type = "BL", DocNo = "", BaseDoc = "", BaseDocNo = "",
            CardCode = "", CardName = "", ItemCode = "", ItemName = "", Quantity = 0m, DeliveryQty = 0m,
            UoM = "Kg", PackType = "", WhsCode = "", WhsName = "", DocStatus = "O", BlNo = "BL12345",
            IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _spkRepo.GetByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.Contains(30L)), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns([spkWithZeroQty]);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 5, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: 30,
            UomMasterId: null);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], [30]);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().NotThrowAsync();
        await _budgetPlanRepo.Received(1).CreateAsync(
            Arg.Is<BudgetPlan>(p => p.Items.Single().SpkShadowId == 30 && p.Items.Single().Quantity == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SpkOutsideUserAccessibleWarehouses_ThrowsNotFoundException()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, code: "WH-10", location: "Lampung", provinceId: 1);

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        // User 99 is not global and is only assigned warehouse 20 ("WH-20") - not the plan's
        // own warehouse (10/"WH-10"). SPK scoping is by the user's accessible warehouses, so
        // the mocked repo (correctly modelling the real WHERE WhsCode IN (@codes) filter)
        // returns no match for SPK 30 which lives outside that scope.
        _rbacService.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(99, Arg.Any<CancellationToken>()).Returns(new List<long> { 20 });
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 20 })), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "WH-20" });
        _spkRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-20" })), Arg.Any<CancellationToken>())
            .Returns(new List<SpkShadow>());

        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [], [30]);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_SpkInDifferentWarehouseThanPlan_ButUserHasAccess_Succeeds()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, code: "WH-10", location: "Lampung", provinceId: 1);
        var spkInOtherWarehouse = new SpkShadow
        {
            Id = 30, CompanyId = 1, Type = "BL", DocNo = "", BaseDoc = "", BaseDocNo = "",
            CardCode = "", CardName = "", ItemCode = "", ItemName = "", Quantity = 0m, DeliveryQty = 0m,
            UoM = "Kg", PackType = "", WhsCode = "WH-20", WhsName = "", DocStatus = "O", BlNo = "BL12345",
            IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());
        // User 99 is assigned both WH-10 (the plan's warehouse) and WH-20 (the SPK's warehouse) -
        // attaching an SPK from a warehouse other than the plan's own is allowed as long as the
        // user can access that warehouse.
        _rbacService.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(99, Arg.Any<CancellationToken>()).Returns(new List<long> { 10, 20 });
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 10, 20 })), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "WH-10", "WH-20" });
        _spkRepo.GetByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.Contains(30L)), Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-10", "WH-20" })), Arg.Any<CancellationToken>())
            .Returns(new List<SpkShadow> { spkInOtherWarehouse });
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<VendorShadow>());

        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [], [30]);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_ActiveWarehouseHeaderSetToOtherWarehouse_SpkFromAccessibleWarehouse_Succeeds()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, code: "WH-10", location: "Lampung", provinceId: 1);
        var spkInOtherWarehouse = new SpkShadow
        {
            Id = 30, CompanyId = 1, Type = "BL", DocNo = "", BaseDoc = "", BaseDocNo = "",
            CardCode = "", CardName = "", ItemCode = "", ItemName = "", Quantity = 0m, DeliveryQty = 0m,
            UoM = "Kg", PackType = "", WhsCode = "WH-20", WhsName = "", DocStatus = "O", BlNo = "BL12345",
            IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());
        // The caller's ambient X-Warehouse-Id header is scoped to WH-10 only (e.g. the FE's
        // "currently active" warehouse elsewhere in the UI), but the SPK being attached lives in
        // WH-20, which the user is separately authorized for. SPK attachment must not be narrowed
        // by this header - it should follow the same full-access-list resolution as browsing/listing
        // SPKs, not whatever single warehouse happens to be active in an unrelated header.
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(10L);
        _rbacService.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(99, Arg.Any<CancellationToken>()).Returns(new List<long> { 10, 20 });
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 10, 20 })), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "WH-10", "WH-20" });
        _spkRepo.GetByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.Contains(30L)), Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-10", "WH-20" })), Arg.Any<CancellationToken>())
            .Returns(new List<SpkShadow> { spkInOtherWarehouse });
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns(new List<VendorShadow>());

        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [], [30]);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddSpkItemAsync_SpkOutsideUserAccessibleWarehouses_ThrowsNotFoundException()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            WarehouseShadowId = 10,
            Warehouse = TestBuilders.WarehouseShadow(id: 10, code: "WH-10"),
            SpkItems = [],
        };
        _budgetPlanRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        _rbacService.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(99, Arg.Any<CancellationToken>()).Returns(new List<long> { 20 });
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 20 })), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "WH-20" });
        _spkRepo.GetByIdAsync(30, Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-20" })), Arg.Any<CancellationToken>())
            .Returns((SpkShadow?)null);

        var request = new AddSpkItemRequest(SpkShadowId: 30);

        var act = () => _sut.AddSpkItemAsync(1, request, userId: 99);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddSpkItemAsync_ActiveWarehouseHeaderSetToOtherWarehouse_SpkFromAccessibleWarehouse_Succeeds()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            WarehouseShadowId = 10,
            Warehouse = TestBuilders.WarehouseShadow(id: 10, code: "WH-10"),
            SpkItems = [],
        };
        var spkInOtherWarehouse = new SpkShadow
        {
            Id = 30, CompanyId = 1, Type = "BL", DocNo = "", BaseDoc = "", BaseDocNo = "",
            CardCode = "", CardName = "", ItemCode = "", ItemName = "", Quantity = 0m, DeliveryQty = 0m,
            UoM = "Kg", PackType = "", WhsCode = "WH-20", WhsName = "", DocStatus = "O", BlNo = "BL12345",
            IsActive = true, FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };
        _budgetPlanRepo.GetByIdWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(plan);
        // Ambient header narrows to WH-10 (the plan's own warehouse), but the SPK is in WH-20,
        // which the user also has access to. That header must not block the attach.
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(10L);
        _rbacService.HasGlobalAccessAsync(99, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(99, Arg.Any<CancellationToken>()).Returns(new List<long> { 10, 20 });
        _warehouseRepo.GetCodesByIdsAsync(Arg.Is<IEnumerable<long>>(ids => ids.SequenceEqual(new long[] { 10, 20 })), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "WH-10", "WH-20" });
        _spkRepo.GetByIdAsync(30, Arg.Is<IReadOnlyList<string>>(codes => codes.SequenceEqual(new[] { "WH-10", "WH-20" })), Arg.Any<CancellationToken>())
            .Returns(spkInOtherWarehouse);

        var request = new AddSpkItemRequest(SpkShadowId: 30);

        var act = () => _sut.AddSpkItemAsync(1, request, userId: 99);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateAsync_Item_ThrowsNotFoundException_WhenUomMasterIdNotFound()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem { Id = 1, ItemShadowId = 11, UomMasterId = 55, CostValue = 100m };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 1, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: null,
            UomMasterId: 999);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], null);

        var act = () => _sut.CreateAsync(99, request);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*UoM 999*");
    }

    [Fact]
    public async Task CreateAsync_ItemWithRateCardTax_SnapshotsRateAndComputesAmounts()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem
        {
            Id = 1,
            ItemShadowId = 11,
            UomMasterId = 55,
            CostValue = 100m,
            PpnTaxTypeCode = "PPN11", PpnRate = 11m,
            PphTaxTypeCode = "PPH23", PphRate = 2m,
        };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 2, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: null,
            UomMasterId: null);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.CreateAsync(99, request, ct);

        // TotalValue = CostValue(100) * Quantity(2) = 200
        // PpnAmount = 200 * 11% = 22.00, PphAmount = 200 * 2% = 4.00, GrandTotal = 200 + 22 - 4 = 218.00
        await _budgetPlanRepo.Received(1).CreateAsync(
            Arg.Is<BudgetPlan>(p =>
                p.Items.Single().PpnTaxTypeCode == "PPN11" &&
                p.Items.Single().PpnRate == 11m &&
                p.Items.Single().PphTaxTypeCode == "PPH23" &&
                p.Items.Single().PphRate == 2m &&
                p.Items.Single().PpnAmount == 22.00m &&
                p.Items.Single().PphAmount == 4.00m &&
                p.Items.Single().GrandTotal == 218.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_copies_CostTreatment_from_rate_item_to_budget_item()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem
        {
            Id = 1,
            ItemShadowId = 11,
            UomMasterId = 55,
            CostValue = 100m,
            CostTreatment = CostTreatments.TidakDibiayakan,
        };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        BudgetPlan? captured = null;
        _budgetPlanRepo.CreateAsync(Arg.Do<BudgetPlan>(bp => captured = bp), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 1, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: null,
            UomMasterId: null);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.CreateAsync(99, request, ct);

        captured!.Items.Should().ContainSingle()
            .Which.CostTreatment.Should().Be(CostTreatments.TidakDibiayakan);
    }

    [Fact]
    public async Task CreateAsync_ItemWithNoRateCardTax_ZerosOutTaxAmounts()
    {
        var template = new BudgetTemplate
        {
            Id = 5, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, ProvinceId = 1, Items = []
        };
        var warehouse = TestBuilders.WarehouseShadow(id: 10, location: "Lampung", provinceId: 1);
        var rateItem = new RateCardItem { Id = 1, ItemShadowId = 11, UomMasterId = 55, CostValue = 100m };

        _budgetTemplateRepo.GetByIdForPlanSourceAsync(5, Arg.Any<CancellationToken>()).Returns(template);
        _warehouseRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(warehouse);
        _userRepo.CheckWarehouseAccessAsync(99, 10, Arg.Any<CancellationToken>()).Returns((true, true));
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 22 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(22, 11)] = rateItem });
        _uomRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _budgetPlanRepo.GetByIdProjectionAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        var item = new CreateBudgetPlanItemRequest(
            ItemShadowId: 11, ActivityTypeId: 3, VendorShadowId: 22,
            Quantity: 1, CostValue: null, Type: BudgetPlanType.External,
            IsRfba: false,
            BillOfLading: null, Description: null, SpkShadowId: null,
            UomMasterId: null);
        var request = new CreateBudgetPlanRequest(5, 10, null, DateTime.UtcNow, [item], null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.CreateAsync(99, request, ct);

        await _budgetPlanRepo.Received(1).CreateAsync(
            Arg.Is<BudgetPlan>(p =>
                p.Items.Single().PpnAmount == 0m &&
                p.Items.Single().PphAmount == 0m &&
                p.Items.Single().GrandTotal == p.Items.Single().TotalValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidationException_WhenRemovingItemWithWorkOrders()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500 },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var request = new UpdateBudgetPlanRequest(null, null, null, Items: [], SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidationException_WhenReducingItemBelowCommittedTotal()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500 },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        var request = new UpdateBudgetPlanRequest(
            null, null, null,
            Items: [new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 5m, CostValue: 10m, BudgetPlanType.External, false, null, null, null)],
            SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // Boundary case for the guard added in UpdateAsync: an item with existing WorkOrders whose
    // incoming total is held exactly equal to (not below) the committed total must NOT throw.
    // Catches an off-by-one comparison slip (e.g. `<=` instead of `<`) that the two throw-path
    // tests above cannot detect.
    [Fact]
    public async Task UpdateAsync_Succeeds_WhenRaisingItemWithWorkOrdersAboveCommittedTotal()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500 },
            ],
        };
        var template = new BudgetTemplate { Id = 1, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, Items = [] };
        var rateItem = new RateCardItem { Id = 1, ItemShadowId = 500, UomMasterId = 55, CostValue = 10m };

        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _budgetTemplateRepo.GetByIdForPlanSourceAsync(1, Arg.Any<CancellationToken>()).Returns(template);
        _vendorRepo.GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>())
            .Returns([new VendorShadow { Id = 1 }]);
        _rateCardRepo.FindSubmittedRatesBatchAsync(Arg.Any<IReadOnlyList<(long, long)>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<(long, long), RateCardItem> { [(1, 500)] = rateItem });
        _budgetPlanRepo.GetByIdProjectionAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());

        // Incoming total (10 * 10 = 100) equals the committed total exactly - not a reduction.
        var request = new UpdateBudgetPlanRequest(
            null, null, null,
            Items: [new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 10m, CostValue: 10m, BudgetPlanType.External, false, null, null, null)],
            SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().NotThrowAsync();
        await _budgetPlanRepo.Received(1).UpdateAsync(plan, Arg.Any<CancellationToken>());
        // Regression guard for the FK-violation bug: the WorkOrder-linked item must be updated
        // in place, never removed-then-recreated (a mocked repo can't otherwise surface the
        // Restrict-FK violation that plan.Items.Clear() would trigger against a real DB).
        plan.Items.Should().ContainSingle(i => i.Id == 100);
        plan.Items.Single(i => i.Id == 100).TotalValue.Should().Be(100m);
    }

    // Finding 1: the in-place mutation path for WorkOrder-linked items must reject a non-positive
    // CostValue override, mirroring AddItemsAsync's UnitCostOverrideMustBePositive check - otherwise
    // a caller could silently push a negative TotalValue/GrandTotal onto a committed item.
    [Fact]
    public async Task UpdateAsync_ThrowsValidationException_WhenWorkOrderLinkedItemCostValueIsNotPositive()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                // Soft-deleted: still triggers the in-place mutation path (ANY WorkOrder, active or
                // not), but is excluded from the "committed total" comparison, so this test isolates
                // the positivity check rather than tripping the earlier CannotReduceItemBelowCommitted guard.
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500, DeletedAt = DateTime.UtcNow },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _budgetTemplateRepo.GetByIdForPlanSourceAsync(1, Arg.Any<CancellationToken>())
            .Returns(new BudgetTemplate { Id = 1, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, Items = [] });

        var request = new UpdateBudgetPlanRequest(
            null, null, null,
            Items: [new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 20m, CostValue: 0m, BudgetPlanType.External, false, null, null, null)],
            SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // Finding 2: two incoming rows sharing a WorkOrder-linked ItemShadowId can sum-pass the
    // committed-total check (60 + 60 = 120, not below committed = 100) while the in-place mutation
    // loop only applies the first matching row (TotalValue = 60) - silently persisting BELOW the
    // committed total the guard exists to protect. Must throw instead of silently under-applying.
    [Fact]
    public async Task UpdateAsync_ThrowsValidationException_WhenWorkOrderLinkedItemIsSplitAcrossMultipleRows()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500 },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        // Two rows for ItemShadowId 500: 6 * 10 = 60 and 6 * 10 = 60, summing to 120 (>= committed
        // 100), but only the first would actually be applied by the in-place mutation loop.
        var request = new UpdateBudgetPlanRequest(
            null, null, null,
            Items:
            [
                new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 6m, CostValue: 10m, BudgetPlanType.External, false, null, null, null),
                new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 6m, CostValue: 10m, BudgetPlanType.External, false, null, null, null),
            ],
            SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // The core regression guard for the "delete-and-recreate churns WorkOrder-linked rows" bug:
    // raising a WorkOrder-linked item's amount must update the existing BudgetPlanItem row in
    // place (same Id), never remove it from plan.Items - removal is what trips the Restrict FK
    // against WorkOrder.BudgetPlanItemId on a real DB commit.
    [Fact]
    public async Task UpdateAsync_UpdatesWorkOrderLinkedItemInPlace_WhenRaisingAmount()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500 },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _budgetPlanRepo.GetByIdProjectionAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());
        // UpdateAsync loads the template unconditionally whenever request.Items is not null, even
        // though this test's item is entirely handled by the in-place update path.
        _budgetTemplateRepo.GetByIdForPlanSourceAsync(1, Arg.Any<CancellationToken>())
            .Returns(new BudgetTemplate { Id = 1, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, Items = [] });

        // Raise the amount from 100 to 200 (10 qty x 20 cost).
        var request = new UpdateBudgetPlanRequest(
            null, null, null,
            Items: [new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 10m, CostValue: 20m, BudgetPlanType.External, false, null, null, null)],
            SpkShadowIds: null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.UpdateAsync(plan.Id, request, 99, ct);

        plan.Items.Should().ContainSingle(i => i.Id == 100);
        var updated = plan.Items.Single(i => i.Id == 100);
        updated.CostValue.Should().Be(20m);
        updated.TotalValue.Should().Be(200m);
        // AddItemsAsync's vendor/rate-card enrichment path must not run for an in-place update.
        await _vendorRepo.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<long>>(), Arg.Any<CancellationToken>());
    }

    // Regression guard: the committed-total pre-check must resolve an omitted CostValue against the
    // existing item's rate, exactly like the in-place mutation does - otherwise a quantity-only raise
    // that omits CostValue is miscomputed as (null ?? 0) * Quantity and falsely rejected as a
    // reduction below committed spend, even though the real applied total only goes up.
    [Fact]
    public async Task UpdateAsync_Succeeds_WhenRaisingQuantityWithCostValueOmitted()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500 },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _budgetPlanRepo.GetByIdProjectionAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());
        _budgetTemplateRepo.GetByIdForPlanSourceAsync(1, Arg.Any<CancellationToken>())
            .Returns(new BudgetTemplate { Id = 1, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, Items = [] });

        // Quantity raised 10 -> 20, CostValue omitted (keeps existing rate of 10):
        // real new total = 200, an increase - must not throw CannotReduceItemBelowCommitted.
        var request = new UpdateBudgetPlanRequest(
            null, null, null,
            Items: [new CreateBudgetPlanItemRequest(500, 3, 1, Quantity: 20m, CostValue: null, BudgetPlanType.External, false, null, null, null)],
            SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().NotThrowAsync();
        var updated = plan.Items.Single(i => i.Id == 100);
        updated.CostValue.Should().Be(10m);
        updated.TotalValue.Should().Be(200m);
    }

    // A soft-deleted WorkOrder still physically holds a live Restrict FK to its BudgetPlanItem
    // row - the FK-safety check must protect the item from removal regardless of DeletedAt, even
    // though a soft-deleted WorkOrder no longer counts toward the "committed total" business rule.
    [Fact]
    public async Task UpdateAsync_ThrowsValidationException_WhenRemovingItemWithOnlySoftDeletedWorkOrder()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders =
            [
                new WorkOrder { Id = 900, BudgetPlanId = 1, BudgetPlanItemId = 100, ItemShadowId = 500, DeletedAt = DateTime.UtcNow },
            ],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);

        // Omit item 500 entirely from the incoming request.
        var request = new UpdateBudgetPlanRequest(null, null, null, Items: [], SpkShadowIds: null);

        var act = () => _sut.UpdateAsync(plan.Id, request, 99);

        await act.Should().ThrowAsync<ValidationException>();
    }

    // Regression check that the FK-safety fix didn't over-broaden protection: an item with no
    // WorkOrder at all (active or deleted) must remain freely removable.
    [Fact]
    public async Task UpdateAsync_RemovesItem_WhenItHasNoWorkOrders()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Status = BudgetPlanStatus.Rejected,
            BudgetTemplateId = 1,
            BudgetTemplate = new BudgetTemplate { Id = 1 },
            Warehouse = TestBuilders.WarehouseShadow(),
            Items =
            [
                new BudgetPlanItem { Id = 100, BudgetPlanId = 1, ItemShadowId = 500, CostValue = 10m, Quantity = 10m, TotalValue = 100m },
            ],
            WorkOrders = [],
        };
        _budgetPlanRepo.GetByIdWithItemsAndWorkOrdersAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _budgetPlanRepo.GetByIdProjectionAsync(plan.Id, Arg.Any<CancellationToken>())
            .Returns(MinimalBudgetPlanResponse());
        _budgetTemplateRepo.GetByIdForPlanSourceAsync(1, Arg.Any<CancellationToken>())
            .Returns(new BudgetTemplate { Id = 1, CompanyId = 1, Status = BudgetTemplateStatus.Submitted, Items = [] });

        var request = new UpdateBudgetPlanRequest(null, null, null, Items: [], SpkShadowIds: null);

        var ct = TestContext.Current.CancellationToken;
        await _sut.UpdateAsync(plan.Id, request, 99, ct);

        plan.Items.Should().BeEmpty();
    }

    private static BudgetPlanResponse MinimalBudgetPlanResponse() => new(
        Id: 1,
        BudgetNo: "BP.2601000001",
        Template: new BudgetTemplateSummaryInfo(1, "T001", null, null, null),
        WarehouseCode: "WH01",
        WarehouseName: "Main",
        Remark: null,
        DocDate: DateTime.UtcNow,
        Status: "Draft",
        StatusDisplay: "Draft",
        SpkItems: [],
        Items: [],
        GrandTotal: 0,
        TotalPpnAmount: 0,
        TotalPphAmount: 0,
        TaxInclusiveGrandTotal: 0,
        CreatedAt: DateTime.UtcNow,
        CreatedByName: "User",
        SubmittedAt: null,
        SubmittedByName: null,
        Approval: new BudgetPlanApprovalInfo(0, 0, []),
        RejectedAt: null,
        RejectedByName: null,
        RejectionReason: null);

    private static BudgetPlanItemResponse BudgetPlanItem(long id, long vendorShadowId) => new(
        Id: id,
        ItemShadowId: 1,
        CostDetail: "ITEM-1",
        CostName: "Item",
        Coa: "501010206",
        CoaName: "Cost",
        VendorShadowId: vendorShadowId,
        VendorCode: $"V-{vendorShadowId}",
        VendorName: $"Vendor {vendorShadowId}",
        UomMasterId: 1,
        UomCode: "PCS",
        UomName: "Pieces",
        CostValue: 100m,
        Quantity: 1m,
        TotalValue: 100m,
        SortOrder: 0,
        Type: "External",
        IsRfba: false,
        DocExternal: null,
        BillOfLading: null,
        Description: null,
        ActivityTypeId: 1,
        ActivityTypeCode: null,
        ActivityTypeName: null,
        SpkShadowId: null,
        PpnTaxTypeCode: null,
        PpnRate: 0m,
        PphTaxTypeCode: null,
        PphRate: 0m,
        PpnAmount: 0m,
        PphAmount: 0m,
        GrandTotal: 100m,
        CostTreatment: null);

    [Fact]
    public async Task SubmitAsync_ResetsRecapToPending_WhenResubmittingRejectedPlan()
    {
        var plan = new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            WarehouseShadowId = 5,
            Status = BudgetPlanStatus.Rejected,
            WorkflowInstanceId = 77,
            Items = [new BudgetPlanItem { Id = 100, ItemShadowId = 500, TotalValue = 100m }],
        };
        _budgetPlanRepo.GetByIdForSubmitAsync(plan.Id, Arg.Any<CancellationToken>()).Returns(plan);
        _userRepo.CheckWarehouseAccessAsync(Arg.Any<long>(), plan.WarehouseShadowId, Arg.Any<CancellationToken>())
            .Returns((true, true));

        var template = new WorkflowTemplate
        {
            Id = 1,
            Stages = [new WorkflowStage { StageOrder = 1, StageName = "Approval", ApproverRoles = ["SA"] }],
        };
        _workflowRepo.GetActiveTemplateAsync(plan.CompanyId, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(template);
        _workflowRepo.GetInstanceWithStagesAsync(77, Arg.Any<CancellationToken>())
            .Returns(new WorkflowInstance { Id = 77, Stages = [] });

        var ct = TestContext.Current.CancellationToken;
        await _sut.SubmitAsync(plan.Id, userId: 42, ct: ct);

        await _recapRepo.Received(1).ResetToPendingByBudgetPlanIdAsync(plan.Id, Arg.Any<CancellationToken>());
        await _auditLogWriter.Received(1).LogAsync(
            action: "DELETE",
            tableName: "workflow_instances",
            recordId: 77,
            userId: 42,
            userEmail: Arg.Any<string?>(),
            userFullname: Arg.Any<string?>(),
            companyId: plan.CompanyId,
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Is<string>(s => s.Contains("\"Id\":77") && s.Contains("\"CurrentStageOrder\":0")),
            newValues: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    // Builds a 2-stage workflow plan with stage 1 or 2 pending.
    private static BudgetPlan BuildPlanWithWorkflow(BudgetPlanStatus status, int pendingStageOrder)
    {
        var stage1 = new WorkflowInstanceStage
        {
            Id = 1,
            StageOrder = 1,
            StageName = "Warehouse Head Approval",
            ApproverRoles = ["WAREHOUSE_HEAD"],
            Status = pendingStageOrder == 1 ? WorkflowStageStatus.Pending : WorkflowStageStatus.Approved,
        };
        var stage2 = new WorkflowInstanceStage
        {
            Id = 2,
            StageOrder = 2,
            StageName = "Coordinator WH Approval",
            ApproverRoles = ["COORDINATOR_WH"],
            Status = WorkflowStageStatus.Pending,
        };
        var instance = new WorkflowInstance
        {
            Id = 100,
            DocType = WorkflowDocTypes.BudgetPlanApproval,
            DocId = 1,
            CurrentStageOrder = pendingStageOrder,
            Stages = [stage1, stage2],
        };
        return new BudgetPlan
        {
            Id = 1,
            CompanyId = 1,
            Code = "BP.2604000001",
            Status = status,
            CreatedByUserId = 99,
            SubmittedByUserId = 88,
            CreatedBy = TestBuilders.ActiveUser(id: 99),
            WorkflowInstanceId = 100,
            WorkflowInstance = instance,
            WarehouseShadowId = 10,
            Warehouse = TestBuilders.WarehouseShadow(id: 10),
            BudgetTemplate = new BudgetTemplate
            {
                Id = 5,
                CompanyId = 1,
                ProvinceId = 1,
            },
            Items = [],
            SpkItems = []
        };
    }
}
