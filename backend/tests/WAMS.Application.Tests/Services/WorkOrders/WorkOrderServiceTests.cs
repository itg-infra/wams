namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using WAMS.Application.DTOs.BudgetPlans;
using WAMS.Application.DTOs.WorkOrders;
using WAMS.Application.Interfaces.AuditLogs;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.RecapWorkOrders;
using WAMS.Application.Interfaces.TransportOrders;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Interfaces.WorkOrders;
using WAMS.Application.Services.WorkOrders;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using WAMS.Domain.ValueObjects;
using Xunit;

public class WorkOrderServiceTests
{
    private readonly IWorkOrderRepository _woRepo = Substitute.For<IWorkOrderRepository>();
    private readonly IBudgetPlanRepository _bpRepo = Substitute.For<IBudgetPlanRepository>();
    private readonly ITransportOrderShadowRepository _toRepo = Substitute.For<ITransportOrderShadowRepository>();
    private readonly IRecapWorkOrderRepository _recapRepo = Substitute.For<IRecapWorkOrderRepository>();
    private readonly IWarehouseContext _warehouseCtx = Substitute.For<IWarehouseContext>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly ICodeCounterRepository _codeCounterRepo = Substitute.For<ICodeCounterRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IWamsMetrics _metrics = Substitute.For<IWamsMetrics>();
    private readonly IAuditLogWriter _auditWriter = Substitute.For<IAuditLogWriter>();
    private readonly IValidator<UpdateWorkOrderRequest> _updateValidator = Substitute.For<IValidator<UpdateWorkOrderRequest>>();
    private readonly WorkOrderService _sut;

    public WorkOrderServiceTests()
    {
        _updateValidator.Validate(Arg.Any<UpdateWorkOrderRequest>())
            .Returns(new ValidationResult());

        _sut = new WorkOrderService(
            _woRepo, _bpRepo, _toRepo, _recapRepo,
            _warehouseCtx, _userRepo, _rbacService,
            _codeCounterRepo, _uow, _metrics,
            _updateValidator,
            _auditWriter);
    }

    // --- BulkCreateDraftAsync ---

    [Fact]
    public async Task BulkCreateDraftAsync_CopiesRfbaPerBpItem()
    {
        var bp = new BpForWoCreateProjection(
            Id: 1,
            Status: "Approved",
            CompanyId: 42,
            WarehouseShadowId: 10,
            TemplateCode: "TPL01",
            Items:
            [
                new BpItemForWo(Id: 5, ItemShadowId: 11, ActivityTypeCode: "K.GUDANG", IsRfba: true),
                new BpItemForWo(Id: 6, ItemShadowId: 12, ActivityTypeCode: "AT.SPECIFIC", IsRfba: false),
            ]);

        _bpRepo.GetForWoCreateAsync(1, Arg.Any<CancellationToken>()).Returns(bp);
        _woRepo.HasActiveWorkOrderForItemAsync(5, Arg.Any<CancellationToken>()).Returns(false);
        _woRepo.HasActiveWorkOrderForItemAsync(6, Arg.Any<CancellationToken>()).Returns(false);
        _codeCounterRepo.NextRangeAsync(Arg.Any<string>(), 2, Arg.Any<CancellationToken>()).Returns(1L);

        await _sut.BulkCreateDraftAsync(budgetPlanId: 1, actorUserId: 99, ct: TestContext.Current.CancellationToken);

        await _woRepo.Received(1).BulkInsertAsync(
            Arg.Is<IReadOnlyList<WorkOrder>>(list =>
                list.Count == 2
                && list[0].BudgetPlanItemId == 5
                && list[0].IsRfba
                && list[0].ActivityTypeCode == "K.GUDANG"
                && list[0].Code.StartsWith("WO-")
                && list[0].PicUserId == null
                && list[1].BudgetPlanItemId == 6
                && !list[1].IsRfba
                && list[1].ActivityTypeCode == "AT.SPECIFIC"
                && list[1].PicUserId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkCreateDraftAsync_SkipsItemsWithExistingWo()
    {
        var bp = new BpForWoCreateProjection(
            Id: 1,
            Status: "Approved",
            CompanyId: 42,
            WarehouseShadowId: 10,
            TemplateCode: "TPL01",
            Items:
            [
                new BpItemForWo(Id: 5, ItemShadowId: 11, ActivityTypeCode: "K.GUDANG"),
                new BpItemForWo(Id: 6, ItemShadowId: 12, ActivityTypeCode: "K.GUDANG"),
            ]);

        _bpRepo.GetForWoCreateAsync(1, Arg.Any<CancellationToken>()).Returns(bp);
        _woRepo.HasActiveWorkOrderForItemAsync(5, Arg.Any<CancellationToken>()).Returns(true);  // already has WO
        _woRepo.HasActiveWorkOrderForItemAsync(6, Arg.Any<CancellationToken>()).Returns(false);
        _codeCounterRepo.NextRangeAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>()).Returns(7L);

        await _sut.BulkCreateDraftAsync(budgetPlanId: 1, actorUserId: 99, ct: TestContext.Current.CancellationToken);

        await _woRepo.Received(1).BulkInsertAsync(
            Arg.Is<IReadOnlyList<WorkOrder>>(list => list.Count == 1 && list[0].BudgetPlanItemId == 6),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkCreateDraftAsync_AllItemsHaveWo_DoesNotCallBulkInsert()
    {
        var bp = new BpForWoCreateProjection(
            Id: 1,
            Status: "Approved",
            CompanyId: 42,
            WarehouseShadowId: 10,
            TemplateCode: "TPL01",
            Items: [new BpItemForWo(Id: 5, ItemShadowId: 11, ActivityTypeCode: "K.GUDANG")]);

        _bpRepo.GetForWoCreateAsync(1, Arg.Any<CancellationToken>()).Returns(bp);
        _woRepo.HasActiveWorkOrderForItemAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        await _sut.BulkCreateDraftAsync(budgetPlanId: 1, actorUserId: 99, ct: TestContext.Current.CancellationToken);

        await _woRepo.DidNotReceive().BulkInsertAsync(Arg.Any<IReadOnlyList<WorkOrder>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkCreateDraftAsync_BpNotFound_DoesNotCallBulkInsert()
    {
        _bpRepo.GetForWoCreateAsync(99, Arg.Any<CancellationToken>()).ReturnsNull();

        await _sut.BulkCreateDraftAsync(budgetPlanId: 99, actorUserId: 1, ct: TestContext.Current.CancellationToken);

        await _woRepo.DidNotReceive().BulkInsertAsync(Arg.Any<IReadOnlyList<WorkOrder>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WritesAuditLog_WithDeleteAction()
    {
        var wo = new WorkOrder { Id = 5, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3 };
        _woRepo.GetByIdForUpdateAsync(5, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _userRepo.CheckWarehouseAccessAsync(55, 3, TestContext.Current.CancellationToken).Returns((true, true));

        await _sut.DeleteAsync(5, userId: 55, ct: TestContext.Current.CancellationToken);

        await _auditWriter.Received(1).LogAsync(
            action: "DELETE",
            tableName: "work_orders",
            recordId: 5,
            userId: 55,
            userEmail: Arg.Any<string?>(),
            userFullname: Arg.Any<string?>(),
            companyId: 7,
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Any<string?>(),
            newValues: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_CallerLacksWarehouseAccess_Throws()
    {
        var wo = new WorkOrder { Id = 5, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3 };
        _woRepo.GetByIdForUpdateAsync(5, TestContext.Current.CancellationToken).Returns(wo);
        _userRepo.CheckWarehouseAccessAsync(55, 3, TestContext.Current.CancellationToken).Returns((true, false));

        var act = () => _sut.DeleteAsync(5, userId: 55, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SubmitAsync_WritesAuditLog_WithUpdateAction()
    {
        var wo = new WorkOrder
        {
            Id = 5,
            Status = WorkOrderStatus.Draft,
            BudgetPlanId = 1,
            CompanyId = 7,
            WarehouseShadowId = 3,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(1),
            GpsLocation = new GpsCoordinate(1.0m, 1.0m, 10m, DateTime.UtcNow),
            ActivityTypeCode = "K.GUDANG",
            StorageDetail = new WorkOrderStorageDetail(),
            PicUserId = 1,
        };
        _woRepo.GetByIdForUpdateAsync(5, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _woRepo.GetByIdProjectionAsync(5, TestContext.Current.CancellationToken).Returns(MakeWorkOrderResponse());
        _userRepo.CheckWarehouseAccessAsync(1, 3, TestContext.Current.CancellationToken).Returns((true, true));

        await _sut.SubmitAsync(5, 1, TestContext.Current.CancellationToken);

        await _auditWriter.Received(1).LogAsync(
            action: "UPDATE",
            tableName: "work_orders",
            recordId: 5,
            userId: 1,
            userEmail: Arg.Any<string?>(),
            userFullname: Arg.Any<string?>(),
            companyId: 7,
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Any<string?>(),
            newValues: Arg.Is<string?>(v => v != null && v.Contains("Submitted")),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_CallerLacksWarehouseAccess_Throws()
    {
        var wo = new WorkOrder { Id = 5, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3 };
        _woRepo.GetByIdForUpdateAsync(5, TestContext.Current.CancellationToken).Returns(wo);
        _userRepo.CheckWarehouseAccessAsync(1, 3, TestContext.Current.CancellationToken).Returns((true, false));

        var act = () => _sut.SubmitAsync(5, 1, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SubmitAsync_PicUserIdIsNull_ThrowsValidation()
    {
        var wo = new WorkOrder
        {
            Id = 5,
            Status = WorkOrderStatus.Draft,
            BudgetPlanId = 1,
            CompanyId = 7,
            WarehouseShadowId = 3,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(1),
            GpsLocation = new GpsCoordinate(1.0m, 1.0m, 10m, DateTime.UtcNow),
            PicUserId = null,
        };
        _woRepo.GetByIdForUpdateAsync(5, TestContext.Current.CancellationToken).Returns(wo);
        _userRepo.CheckWarehouseAccessAsync(1, 3, TestContext.Current.CancellationToken).Returns((true, true));

        var act = () => _sut.SubmitAsync(5, 1, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.WorkOrder.PicRequiredBeforeSubmit);
    }

    [Fact]
    public async Task UpdateAsync_WithChildMutations_WritesAuditLog()
    {
        var existingItem = new WorkOrderUnloadingItem
        {
            Id = 1,
            BlNumber = "BL-001",
            ProductName = "Product A",
            Quantity = 100,
            UomCode = "KG",
            SortOrder = 1
        };
        var wo = new WorkOrder
        {
            Id = 10,
            Status = WorkOrderStatus.Draft,
            BudgetPlanId = 1,
            CompanyId = 7,
            WarehouseShadowId = 3
        };
        wo.UnloadingItems.Add(existingItem);

        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _woRepo.GetByIdProjectionAsync(10, TestContext.Current.CancellationToken).Returns(MakeWorkOrderResponse());
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, true));

        var newItem = new CreateUnloadingItemRequest(
            SpkShadowId: null,
            BlNumber: "BL-999",
            ProductName: "Product B",
            Quantity: 200,
            UomCode: "TON",
            NoVehicle: null,
            NoContainer: null,
            NoSeal: null,
            GrossWeight: null,
            FinalWeight: null,
            NettWeight: null,
            TotalBag: null,
            UnitWeight: null,
            IsChecked: false,
            SortOrder: 1);

        var req = new UpdateWorkOrderRequest(
            PicUserId: null,
            StartDate: null,
            EndDate: null,
            CodeBlock: null,
            Notes: null,
            GpsLocation: null,
            TransportOrderShadowIds: null,
            UnloadingItems: [newItem],
            LoadingItems: null,
            Fumigation: null,
            Storage: null,
            Qc: null,
            HeavyEquipment: null,
            Unbagging: null,
            Rebagging: null);

        await _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        await _auditWriter.Received(1).LogAsync(
            action: "UPDATE",
            tableName: "work_orders",
            recordId: 10,
            userId: 42,
            userEmail: Arg.Any<string?>(),
            userFullname: Arg.Any<string?>(),
            companyId: 7,
            ipAddress: Arg.Any<string?>(),
            userAgent: Arg.Any<string?>(),
            oldValues: Arg.Is<string?>(v => v != null),
            newValues: Arg.Is<string?>(v => v != null),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_CallerLacksWarehouseAccess_Throws()
    {
        var wo = new WorkOrder { Id = 10, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3 };
        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, false));

        var req = MakeMinimalUpdateRequest(picUserId: null);

        var act = () => _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UpdateAsync_PicUserHasExecutePermission_UpdatesPic()
    {
        var wo = new WorkOrder { Id = 10, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3, PicUserId = 1 };
        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, true));
        _userRepo.GetUsersByPermissionAndWarehouseAsync(
                7, 3, Permissions.WorkOrder.Execute, TestContext.Current.CancellationToken)
            .Returns([new User { Id = 2, Fullname = "Foreman Two" }]);
        _woRepo.GetByIdProjectionAsync(10, TestContext.Current.CancellationToken).Returns(MakeWorkOrderResponse());

        var req = MakeMinimalUpdateRequest(picUserId: 2);

        await _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        wo.PicUserId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_PicUserIdStartsNull_CanBeAssigned()
    {
        var wo = new WorkOrder { Id = 10, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3, PicUserId = null };
        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, true));
        _userRepo.GetUsersByPermissionAndWarehouseAsync(
                7, 3, Permissions.WorkOrder.Execute, TestContext.Current.CancellationToken)
            .Returns([new User { Id = 2, Fullname = "Foreman Two" }]);
        _woRepo.GetByIdProjectionAsync(10, TestContext.Current.CancellationToken).Returns(MakeWorkOrderResponse());

        var req = MakeMinimalUpdateRequest(picUserId: 2);

        await _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        wo.PicUserId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_PicUserNotEligible_ThrowsValidation()
    {
        var wo = new WorkOrder { Id = 10, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3, PicUserId = 1 };
        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, true));
        _userRepo.GetUsersByPermissionAndWarehouseAsync(
                7, 3, Permissions.WorkOrder.Execute, TestContext.Current.CancellationToken)
            .Returns([]); // requested user exists but holds no workorder.workorder.execute here

        var req = MakeMinimalUpdateRequest(picUserId: 999);

        var act = () => _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_PicUserIdUnchanged_DoesNotQueryEligibility()
    {
        var wo = new WorkOrder { Id = 10, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3, PicUserId = 1 };
        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, true));
        _woRepo.GetByIdProjectionAsync(10, TestContext.Current.CancellationToken).Returns(MakeWorkOrderResponse());

        var req = MakeMinimalUpdateRequest(picUserId: 1); // same as wo.PicUserId

        await _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        await _userRepo.DidNotReceive().GetUsersByPermissionAndWarehouseAsync(
            Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        wo.PicUserId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_OthersSentInsteadOfStorage_PopulatesStorageDetail()
    {
        var wo = new WorkOrder { Id = 10, Status = WorkOrderStatus.Draft, BudgetPlanId = 1, CompanyId = 7, WarehouseShadowId = 3 };
        _woRepo.GetByIdForUpdateAsync(10, TestContext.Current.CancellationToken).Returns(wo);
        _recapRepo.IsApprovedByBudgetPlanIdAsync(1, TestContext.Current.CancellationToken).Returns(false);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken).Returns((true, true));
        _woRepo.GetByIdProjectionAsync(10, TestContext.Current.CancellationToken).Returns(MakeWorkOrderResponse());

        var req = MakeMinimalUpdateRequest(picUserId: null) with
        {
            Others = new CreateStorageDetailRequest(
                HasPindahStapel: true,
                HasPembersihan: true,
                HasPerapihan: true,
                VolumeWeight: 2121,
                WorkerOnDuty: 221,
                HasMask: true,
                HasSafetyGlasses: true,
                HasHandGloves: true,
                HasHelmet: true,
                HasSafetyShoes: true,
                HasSafetyVest: true)
        };

        await _sut.UpdateAsync(10, req, userId: 42, ct: TestContext.Current.CancellationToken);

        wo.StorageDetail.Should().NotBeNull();
        wo.StorageDetail!.VolumeWeight.Should().Be(2121);
        wo.StorageDetail!.WorkerOnDuty.Should().Be(221);
        wo.StorageDetail!.HasMask.Should().BeTrue();
    }

    // --- GetPicCandidatesAsync ---

    [Fact]
    public async Task GetPicCandidatesAsync_ReturnsUsersWithExecutePermission()
    {
        var context = new WorkOrderPicContext(CompanyId: 7, WarehouseShadowId: 3);
        _woRepo.GetPicContextAsync(10, TestContext.Current.CancellationToken).Returns(context);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken)
            .Returns((true, true));
        _userRepo.GetUsersByPermissionAndWarehouseAsync(
                7, 3, Permissions.WorkOrder.Execute, TestContext.Current.CancellationToken)
            .Returns([new User { Id = 2, Fullname = "Foreman Two" }]);

        var result = await _sut.GetPicCandidatesAsync(10, userId: 42, ct: TestContext.Current.CancellationToken);

        result.Should().ContainSingle(r => r.Id == 2 && r.Fullname == "Foreman Two");
    }

    [Fact]
    public async Task GetPicCandidatesAsync_WoNotFound_Throws()
    {
        _woRepo.GetPicContextAsync(99, TestContext.Current.CancellationToken).ReturnsNull();

        var act = () => _sut.GetPicCandidatesAsync(99, userId: 42, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetPicCandidatesAsync_CallerLacksWarehouseAccess_Throws()
    {
        var context = new WorkOrderPicContext(CompanyId: 7, WarehouseShadowId: 3);
        _woRepo.GetPicContextAsync(10, TestContext.Current.CancellationToken).Returns(context);
        _userRepo.CheckWarehouseAccessAsync(42, 3, TestContext.Current.CancellationToken)
            .Returns((true, false));

        var act = () => _sut.GetPicCandidatesAsync(10, userId: 42, ct: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    // --- Helpers ---

    private static UpdateWorkOrderRequest MakeMinimalUpdateRequest(long? picUserId) => new(
        PicUserId: picUserId,
        StartDate: null,
        EndDate: null,
        CodeBlock: null,
        Notes: null,
        GpsLocation: null,
        TransportOrderShadowIds: null,
        UnloadingItems: null,
        LoadingItems: null,
        Fumigation: null,
        Storage: null,
        Qc: null,
        HeavyEquipment: null,
        Unbagging: null,
        Rebagging: null);

    private static BpForWoCreateProjection MakeBpProjection() => new(
        Id: 1,
        Status: "Approved",
        CompanyId: 42,
        WarehouseShadowId: 1,
        TemplateCode: "TPL01",
        Items: [new BpItemForWo(Id: 10, ItemShadowId: 1, ActivityTypeCode: "K.GUDANG")]);

    private static WorkOrderResponse MakeWorkOrderResponse() => new(
        Id: 1,
        Code: "WO.2601000001",
        BudgetPlanId: 1,
        BudgetPlanCode: "BP001",
        ActivityTypeCode: "K.GUDANG",
        ActivityTypeDisplay: "Kegiatan Gudang",
        ItemShadowId: 1,
        ActivityName: "Gudang",
        WarehouseShadowId: 1,
        WarehouseCode: "WH01",
        WarehouseName: "Warehouse 1",
        TemplateCode: "TPL01",
        VendorName: null,
        CodeBlock: null,
        PicUserId: 99,
        PicName: "PIC User",
        StartDate: DateTime.UtcNow,
        EndDate: DateTime.UtcNow.AddDays(1),
        IsRfba: false,
        Status: "Draft",
        Notes: null,
        GpsLocation: null,
        ProductName: null,
        Quantity: null,
        UomCode: null,
        BlNumber: null,
        VesselName: null,
        TransportOrders: null,
        UnloadingItems: null,
        LoadingItems: null,
        Fumigation: null,
        Storage: null,
        Qc: null,
        HeavyEquipment: null,
        Unbagging: null,
        Rebagging: null,
        CreatedAt: DateTime.UtcNow,
        CreatedByName: "Creator",
        SubmittedAt: null,
        SubmittedByName: null);
}
