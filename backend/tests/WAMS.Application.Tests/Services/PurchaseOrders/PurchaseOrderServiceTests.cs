namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Interfaces.BudgetPlans;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.PurchaseOrders;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Enums;
using WAMS.Domain.Exceptions;
using Xunit;

public class PurchaseOrderServiceTests
{
    private readonly IPurchaseOrderRepository _poRepo = Substitute.For<IPurchaseOrderRepository>();
    private readonly IBudgetPlanRepository _bpRepo = Substitute.For<IBudgetPlanRepository>();
    private readonly IVendorShadowRepository _vendorRepo = Substitute.For<IVendorShadowRepository>();
    private readonly ISapApiClient _sapClient = Substitute.For<ISapApiClient>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IWarehouseShadowRepository _warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly ICodeCounterRepository _codeCounterRepo = Substitute.For<ICodeCounterRepository>();

    public PurchaseOrderServiceTests()
    {
        _poRepo.LockForEditAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.SoftDeleteAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.MarkGeneratedAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        // Default to a global-access request without an explicit warehouse header.
        // Header-scoped behavior is configured by the individual test that needs it.
        _warehouseContext.IsSet.Returns(false);
        _warehouseContext.WarehouseId.Returns((long?)null);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    private PurchaseOrderService CreateSut() => new(
        _poRepo, _bpRepo, _vendorRepo, _sapClient, _uow, _warehouseContext, _warehouseRepo, _userRepo, _rbacService, _codeCounterRepo);

    private void ConfigurePoScope(long userId, long activeWarehouseId, params long[] accessibleWarehouseIds)
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(activeWarehouseId);
        _warehouseRepo.GetByIdAsync(activeWarehouseId, Arg.Any<CancellationToken>())
            .Returns(new WarehouseShadow { Id = activeWarehouseId });
        _rbacService.HasGlobalAccessAsync(userId, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(accessibleWarehouseIds.ToList());
    }

    private static BudgetPlan ApprovedPlan(
        long id,
        long warehouseId,
        params long[] vendorIds)
        => new()
        {
            Id = id,
            WarehouseShadowId = warehouseId,
            Status = BudgetPlanStatus.Approved,
            Items = vendorIds.Select((vendorId, index) => new BudgetPlanItem
            {
                Id = id * 10 + index,
                BudgetPlanId = id,
                VendorShadowId = vendorId,
            }).ToList(),
        };

    private static PurchaseOrder DraftPoAcrossPlans(
        long id,
        long vendorId,
        params (long BudgetPlanId, long WarehouseId)[] plans)
        => new()
        {
            Id = id,
            VendorShadowId = vendorId,
            Status = PurchaseOrderStatus.Draft,
            Items = plans.Select((plan, index) => new PurchaseOrderItem
            {
                Id = index + 1,
                BudgetPlanItemId = plan.BudgetPlanId * 10,
                BudgetPlanItem = new BudgetPlanItem
                {
                    Id = plan.BudgetPlanId * 10,
                    BudgetPlanId = plan.BudgetPlanId,
                    BudgetPlan = new BudgetPlan
                    {
                        Id = plan.BudgetPlanId,
                        WarehouseShadowId = plan.WarehouseId,
                    },
                },
            }).ToList(),
        };

    private static AvailablePoItemResponse PickerRow(long itemId, long budgetPlanId) => new(
        itemId,
        budgetPlanId,
        $"BP-{budgetPlanId}",
        null,
        new DateTime(2026, 8, 26),
        false,
        103L,
        "WHSBY010",
        "SBY - SPA",
        1L,
        "V-001",
        "Vendor One",
        10L,
        "ITEM-001",
        "Item One",
        "501010206",
        "Freight",
        false,
        null,
        100m,
        1m,
        "PCS",
        "Pieces",
        false,
        null);

    [Fact]
    public async Task GenerateApdpAsync_GeneratedRfbaPo_CallsSapApdpOnlyAndPersistsDocument()
    {
        var po = new PurchaseOrder
        {
            Id = 91,
            Code = "PO-2608910001",
            VendorShadowId = 1,
            Vendor = new VendorShadow { Id = 1, CardCode = "V-001", CardName = "Vendor One" },
            CreatedBy = new User { Id = 7, Fullname = "Test User" },
            Status = PurchaseOrderStatus.Generated,
            SapDocEntry = 7001,
            DocDate = new DateTime(2026, 8, 31),
            Items =
            [
                new PurchaseOrderItem
                {
                    Id = 501,
                    BudgetPlanItemId = 601,
                    IsRfba = true,
                    ItemCode = "ITEM-001",
                    ItemName = "RFBA item",
                    VendorCode = "V-001",
                    CoaCode = "501010206",
                    UomCode = "PCS",
                    CostValue = 100m,
                    Quantity = 2m,
                    TotalValue = 200m,
                    SortOrder = 1,
                    BudgetPlanItem = new BudgetPlanItem
                    {
                        Id = 601,
                        BudgetPlan = new BudgetPlan { Code = "BP-001", WarehouseShadowId = 103 },
                    },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(91, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForApdpGenerationAsync(91, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApdpResult(801));
        _poRepo.MarkApdpGeneratedAsync(91, Arg.Any<string>(), 801, Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateSut().GenerateApdpAsync(91, 7, TestContext.Current.CancellationToken);

        result.Apdp.Should().NotBeNull();
        result.Apdp!.SapDocEntry.Should().Be(801);
        await _sapClient.Received(1).CreateApDownPaymentAsync(
            Arg.Is<SapCreateApdpRequest>(request =>
                request.ApCode == "PO-2608910001" &&
                request.Items.Count == 1 &&
                request.Items[0].BaseEntry == 7001 &&
                request.Items[0].BaseLine == 0),
            Arg.Any<CancellationToken>());
        await _poRepo.Received(1).MarkApdpGeneratedAsync(91, Arg.Any<string>(), 801, Arg.Any<CancellationToken>());
        await _poRepo.DidNotReceive().MarkGeneratedAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateApdpAsync_SapFailure_StoresAtMostDatabaseErrorLengthAndReleasesClaim()
    {
        var po = new PurchaseOrder
        {
            Id = 91,
            Code = "PO-2608910001",
            VendorShadowId = 1,
            Vendor = new VendorShadow { Id = 1, CardCode = "V-001", CardName = "Vendor One" },
            CreatedBy = new User { Id = 7, Fullname = "Test User" },
            Status = PurchaseOrderStatus.Generated,
            SapDocEntry = 7001,
            DocDate = new DateTime(2026, 8, 31),
            Items =
            [
                new PurchaseOrderItem
                {
                    Id = 501,
                    BudgetPlanItemId = 601,
                    IsRfba = true,
                    ItemCode = "ITEM-001",
                    ItemName = "RFBA item",
                    VendorCode = "V-001",
                    CoaCode = "501010206",
                    UomCode = "PCS",
                    CostValue = 100m,
                    Quantity = 2m,
                    TotalValue = 200m,
                    SortOrder = 1,
                    BudgetPlanItem = new BudgetPlanItem
                    {
                        Id = 601,
                        BudgetPlan = new BudgetPlan { Code = "BP-001", WarehouseShadowId = 103 },
                    },
                },
            ],
        };
        var sapError = new string('E', 1200);
        _poRepo.GetByIdWithItemsAsync(91, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForApdpGenerationAsync(91, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>())
            .Returns<SapCreateApdpResult?>(_ => throw new ValidationException(sapError));

        var act = () => CreateSut().GenerateApdpAsync(91, 7, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
        await _poRepo.Received(1).RecordApdpFailureAsync(
            91,
            Arg.Any<string>(),
            Arg.Is<string>(error => error.Length == 1000),
            Arg.Any<CancellationToken>());
        await _poRepo.Received(1).ReleaseApdpGenerationClaimAsync(
            91,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateApdpAsync_DraftPo_RejectsBeforeClaimingOrCallingSap()
    {
        var po = new PurchaseOrder
        {
            Id = 91,
            Status = PurchaseOrderStatus.Draft,
            Items =
            [
                new PurchaseOrderItem
                {
                    IsRfba = true,
                    BudgetPlanItem = new BudgetPlanItem
                    {
                        BudgetPlan = new BudgetPlan { WarehouseShadowId = 103 },
                    },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(91, Arg.Any<CancellationToken>()).Returns(po);

        var act = () => CreateSut().GenerateApdpAsync(91, 7, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.CannotGenerateApdpOnlyGenerated);
        await _poRepo.DidNotReceive().TryClaimForApdpGenerationAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sapClient.DidNotReceive().CreateApDownPaymentAsync(
            Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateApdpAsync_PoWithoutRfba_RejectsBeforeClaimingOrCallingSap()
    {
        var po = new PurchaseOrder
        {
            Id = 91,
            Status = PurchaseOrderStatus.Generated,
            SapDocEntry = 7001,
            Items =
            [
                new PurchaseOrderItem
                {
                    IsRfba = false,
                    BudgetPlanItem = new BudgetPlanItem
                    {
                        BudgetPlan = new BudgetPlan { WarehouseShadowId = 103 },
                    },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(91, Arg.Any<CancellationToken>()).Returns(po);

        var act = () => CreateSut().GenerateApdpAsync(91, 7, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.NoRfbaItemsCannotGenerateApdp);
        await _poRepo.DidNotReceive().TryClaimForApdpGenerationAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sapClient.DidNotReceive().CreateApDownPaymentAsync(
            Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateApdpAsync_WhenAnotherRequestOwnsClaim_ThrowsConflictWithoutCallingSap()
    {
        var po = new PurchaseOrder
        {
            Id = 91,
            Status = PurchaseOrderStatus.Generated,
            SapDocEntry = 7001,
            Items =
            [
                new PurchaseOrderItem
                {
                    IsRfba = true,
                    BudgetPlanItem = new BudgetPlanItem
                    {
                        BudgetPlan = new BudgetPlan { WarehouseShadowId = 103 },
                    },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(91, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForApdpGenerationAsync(91, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => CreateSut().GenerateApdpAsync(91, 7, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage(ErrorMessages.PurchaseOrder.ApdpGenerationInProgress(91));
        await _sapClient.DidNotReceive().CreateApDownPaymentAsync(
            Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateApdpAsync_AlreadyGenerated_ReturnsExistingStateWithoutCallingSap()
    {
        var po = new PurchaseOrder
        {
            Id = 91,
            Code = "PO-2608910001",
            VendorShadowId = 1,
            Vendor = new VendorShadow { Id = 1, CardCode = "V-001", CardName = "Vendor One" },
            CreatedBy = new User { Id = 7, Fullname = "Test User" },
            Status = PurchaseOrderStatus.Generated,
            SapDocEntry = 7001,
            SapApdpDocEntry = 801,
            DocDate = new DateTime(2026, 8, 31),
            Items =
            [
                new PurchaseOrderItem
                {
                    IsRfba = true,
                    GrandTotal = 200m,
                    BudgetPlanItem = new BudgetPlanItem
                    {
                        BudgetPlan = new BudgetPlan { WarehouseShadowId = 103 },
                    },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(91, Arg.Any<CancellationToken>()).Returns(po);

        var result = await CreateSut().GenerateApdpAsync(91, 7, TestContext.Current.CancellationToken);

        result.Apdp.Should().NotBeNull();
        result.Apdp!.Status.Should().Be("Generated");
        result.Apdp.SapDocEntry.Should().Be(801);
        await _poRepo.DidNotReceive().TryClaimForApdpGenerationAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _sapClient.DidNotReceive().CreateApDownPaymentAsync(
            Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AvailablePoItemQuery_DefaultsMatchDataTableContract()
    {
        var query = new AvailablePoItemQuery { BudgetPlanId = 226 };

        query.BudgetPlanId.Should().Be(226);
        query.VendorShadowId.Should().BeNull();
        query.IncludeGenerated.Should().BeFalse();
        query.Page.Should().Be(1);
        query.Limit.Should().Be(20);
    }

    [Fact]
    public async Task GetAvailableItemsAsync_WithGlobalAccessAndWarehouseHeader_PreservesGlobalScope()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(7L);
        _warehouseRepo.GetByIdAsync(7L, Arg.Any<CancellationToken>())
            .Returns(new WarehouseShadow { Id = 7L });
        _rbacService.HasGlobalAccessAsync(9L, Arg.Any<CancellationToken>()).Returns(true);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>())
            .Returns(ApprovedPlan(226L, 7L, 1L));
        _poRepo.GetAvailableItemsForPickerAsync(
                Arg.Any<IReadOnlyCollection<long>>(), 226L, Arg.Any<DataTableQuery>(), false,
                null, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(([], 0));

        await CreateSut().GetAvailableItemsAsync(
            9L,
            new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 1L },
            TestContext.Current.CancellationToken);

        await _poRepo.Received(1).GetAvailableItemsForPickerAsync(
            Arg.Any<IReadOnlyCollection<long>>(),
            226L,
            Arg.Any<DataTableQuery>(),
            false,
            null,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsAsync_WithRestrictedUserAndWarehouseHeader_PreservesAccessibleWarehouseScope()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>())
            .Returns(ApprovedPlan(226L, 103L, 1L));
        _poRepo.GetAvailableItemsForPickerAsync(
                Arg.Any<IReadOnlyCollection<long>>(), 226L, Arg.Any<DataTableQuery>(), false,
                null, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(([], 0));

        await CreateSut().GetAvailableItemsAsync(
            9L,
            new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 1L },
            TestContext.Current.CancellationToken);

        await _poRepo.Received(1).GetAvailableItemsForPickerAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 1L })),
            226L,
            Arg.Is<DataTableQuery>(q =>
                q.GetType() == typeof(DataTableQuery) &&
                q.Page == 1 &&
                q.Limit == 20 &&
                q.Search == null),
            false,
            null,
            Arg.Is<List<long>>(ids => ids.SequenceEqual(new[] { 103L, 110L })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsAsync_MissingWarehouseHeader_FallsBackToAccessibleWarehouses()
    {
        _warehouseContext.IsSet.Returns(false);
        _warehouseContext.WarehouseId.Returns((long?)null);
        _rbacService.HasGlobalAccessAsync(9L, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(9L, Arg.Any<CancellationToken>())
            .Returns([103L, 110L]);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>())
            .Returns(ApprovedPlan(226L, 110L, 1L));
        _poRepo.GetAvailableItemsForPickerAsync(
                Arg.Any<IReadOnlyCollection<long>>(), 226L, Arg.Any<DataTableQuery>(), false,
                null, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var result = await CreateSut().GetAvailableItemsAsync(
            9L,
            new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 1L },
            TestContext.Current.CancellationToken);

        result.Total.Should().Be(0);
        await _poRepo.Received(1).GetAvailableItemsForPickerAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 1L })),
            226L,
            Arg.Any<DataTableQuery>(),
            false,
            null,
            Arg.Is<List<long>>(ids => ids.SequenceEqual(new[] { 103L, 110L })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsAsync_WithoutVendor_ThrowsVendorRequiredValidation()
    {
        var seed = ApprovedPlan(226L, 103L, 1L, 2L, 1L);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>()).Returns(seed);
        ConfigurePoScope(9L, 103L, 103L, 110L);

        var act = () => CreateSut().GetAvailableItemsAsync(
            9L,
            new AvailablePoItemQuery { BudgetPlanId = 226L },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.Validation.Common.VendorRequired);
    }

    [Fact]
    public async Task GetAvailableItemsAsync_SeedNotFound_ThrowsNotFoundException()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>()).Returns((BudgetPlan?)null);

        var act = () => CreateSut().GetAvailableItemsAsync(
            9L, new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 1L },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.NotFoundException>()
            .WithMessage(ErrorMessages.BudgetPlan.NotFound(226L));
    }

    [Fact]
    public async Task GetAvailableItemsAsync_SeedNotApproved_ThrowsValidationException()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        var seed = ApprovedPlan(226L, 103L, 1L);
        seed.Status = BudgetPlanStatus.Draft;
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>()).Returns(seed);

        var act = () => CreateSut().GetAvailableItemsAsync(
            9L, new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 1L },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.ItemPlanNotApproved(226L));
    }

    [Fact]
    public async Task GetAvailableItemsAsync_SeedWarehouseMismatch_ThrowsValidationException()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>())
            .Returns(ApprovedPlan(226L, 110L, 1L));

        var act = () => CreateSut().GetAvailableItemsAsync(
            9L, new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 2L },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.SeedBudgetPlanWarehouseMismatch(226L, 103L));
    }

    [Fact]
    public async Task GetAvailableItemsAsync_SeedVendorMismatch_ThrowsValidationException()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>())
            .Returns(ApprovedPlan(226L, 103L, 1L));

        var act = () => CreateSut().GetAvailableItemsAsync(
            9L, new AvailablePoItemQuery { BudgetPlanId = 226L, VendorShadowId = 2L },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.SeedVendorMismatch(2L, 226L));
    }

    [Fact]
    public async Task GetAvailableItemsAsync_SeedVendorFilter_QueriesOnlyRequestedVendorAndIncludesGenerated()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        _bpRepo.GetByIdWithItemsAsync(226L, Arg.Any<CancellationToken>())
            .Returns(ApprovedPlan(226L, 103L, 1L, 2L));
        _poRepo.GetAvailableItemsForPickerAsync(
                Arg.Any<IReadOnlyCollection<long>>(), 226L, Arg.Any<DataTableQuery>(), true,
                null, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(([], 0));

        await CreateSut().GetAvailableItemsAsync(
            9L,
            new AvailablePoItemQuery
            {
                BudgetPlanId = 226L,
                VendorShadowId = 2L,
                IncludeGenerated = true,
            },
            TestContext.Current.CancellationToken);

        await _poRepo.Received(1).GetAvailableItemsForPickerAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 2L })),
            226L, Arg.Any<DataTableQuery>(), true, null, Arg.Any<List<long>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsForEditAsync_Draft_UsesPoVendorAndMarksLinkedPlansAsSeed()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        _warehouseContext.IsSet.Returns(false);
        _warehouseContext.WarehouseId.Returns((long?)null);
        var po = DraftPoAcrossPlans(99L, 1L, (226L, 103L), (227L, 110L));
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.GetAvailableItemsForPickerAsync(
                Arg.Any<IReadOnlyCollection<long>>(), null, Arg.Any<DataTableQuery>(), true,
                99L, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(([PickerRow(1L, 226L), PickerRow(2L, 228L)], 2));

        var result = await CreateSut().GetAvailableItemsForEditAsync(
            9L,
            99L,
            new EditAvailablePoItemQuery { IncludeGenerated = true, Page = 2, Limit = 5 },
            TestContext.Current.CancellationToken);

        result.Total.Should().Be(2);
        result.Items.Should().SatisfyRespectively(
            row => row.IsSeedBudgetPlan.Should().BeTrue(),
            row => row.IsSeedBudgetPlan.Should().BeFalse());
        await _poRepo.Received(1).GetAvailableItemsForPickerAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new[] { 1L })),
            null,
            Arg.Is<DataTableQuery>(q => q.GetType() == typeof(DataTableQuery) && q.Page == 2 && q.Limit == 5),
            true,
            99L,
            Arg.Is<List<long>>(ids => ids.SequenceEqual(new[] { 103L, 110L })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsForEditAsync_NonDraft_ThrowsValidationException()
    {
        ConfigurePoScope(9L, 103L, 103L, 110L);
        var po = DraftPoAcrossPlans(99L, 1L, (226L, 103L));
        po.Status = PurchaseOrderStatus.Generated;
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);

        var act = () => CreateSut().GetAvailableItemsForEditAsync(
            9L, 99L, new EditAvailablePoItemQuery(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.CannotUpdateOnlyDraft);
    }

    [Fact]
    public async Task GetByIdAsync_WithSiblingPos_PopulatesLinkedBudgetPlansWithSiblings()
    {
        var bp = new BudgetPlan { Id = 10L, Code = "BP-2026-001" };
        var bpi = new BudgetPlanItem { Id = 1L, BudgetPlanId = 10L, BudgetPlan = bp };
        var poItem = new PurchaseOrderItem
        {
            Id = 1L, BudgetPlanItemId = 1L, BudgetPlanItem = bpi, PurchaseOrderId = 99L,
            ItemShadowId = 1L, ItemCode = "I", ItemName = "I", CoaCode = "C", CoaName = "C",
            VendorShadowId = 1L, VendorCode = "V", VendorName = "V",
            UomMasterId = 1L, UomCode = "PCS", UomName = "Pieces",
            CostValue = 100m, Quantity = 1m, TotalValue = 100m, SortOrder = 1,
        };
        var po = new PurchaseOrder
        {
            Id = 99L, Code = "PO-001", VendorShadowId = 1L,
            Vendor = new VendorShadow { CardCode = "V-001", CardName = "Vendor One" },
            CreatedBy = new User { Fullname = "Creator" },
            Status = PurchaseOrderStatus.Draft,
            DocDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            Items = [poItem],
        };

        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.GetPoSummariesByBudgetPlanIdsAsync(
            Arg.Is<List<long>>(l => l.SequenceEqual(new[] { 10L })),
            99L,
            Arg.Any<CancellationToken>())
            .Returns(new List<(long, long, string)> { (10L, 200L, "PO-002") });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(99L, TestContext.Current.CancellationToken);

        result.LinkedBudgetPlans.Should().HaveCount(1);
        result.LinkedBudgetPlans[0].Id.Should().Be(10L);
        result.LinkedBudgetPlans[0].Code.Should().Be("BP-2026-001");
        result.LinkedBudgetPlans[0].PurchaseOrders.Should().HaveCount(1);
        result.LinkedBudgetPlans[0].PurchaseOrders[0].Id.Should().Be(200L);
        result.LinkedBudgetPlans[0].PurchaseOrders[0].Code.Should().Be("PO-002");
    }

    [Fact]
    public async Task GetByIdAsync_NoSiblingPos_LinkedBudgetPlansHasEmptyPoList()
    {
        var bp = new BudgetPlan { Id = 10L, Code = "BP-2026-001" };
        var bpi = new BudgetPlanItem { Id = 1L, BudgetPlanId = 10L, BudgetPlan = bp };
        var poItem = new PurchaseOrderItem
        {
            Id = 1L, BudgetPlanItemId = 1L, BudgetPlanItem = bpi, PurchaseOrderId = 99L,
            ItemShadowId = 1L, ItemCode = "I", ItemName = "I", CoaCode = "C", CoaName = "C",
            VendorShadowId = 1L, VendorCode = "V", VendorName = "V",
            UomMasterId = 1L, UomCode = "PCS", UomName = "Pieces",
            CostValue = 100m, Quantity = 1m, TotalValue = 100m, SortOrder = 1,
        };
        var po = new PurchaseOrder
        {
            Id = 99L, Code = "PO-001", VendorShadowId = 1L,
            Vendor = new VendorShadow { CardCode = "V-001", CardName = "Vendor One" },
            CreatedBy = new User { Fullname = "Creator" },
            Status = PurchaseOrderStatus.Draft,
            DocDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            Items = [poItem],
        };

        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.GetPoSummariesByBudgetPlanIdsAsync(
            Arg.Any<List<long>>(), 99L, Arg.Any<CancellationToken>())
            .Returns(new List<(long, long, string)>());

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(99L, TestContext.Current.CancellationToken);

        result.LinkedBudgetPlans.Should().HaveCount(1);
        result.LinkedBudgetPlans[0].PurchaseOrders.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_CopiesTaxSnapshotFieldsFromBudgetPlanItem()
    {
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001" };
        var bpi = new BudgetPlanItem
        {
            Id = 100,
            BudgetPlanId = 5L,
            BudgetPlan = bp,
            ItemShadowId = 11,
            Item = new ItemShadow { Id = 11, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "ACC-01", AcctName = "Acct Alpha" },
            VendorShadowId = 22,
            Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
            UomMasterId = 33,
            Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
            CostValue = 100m,
            Quantity = 2m,
            TotalValue = 200m,
            PpnTaxTypeCode = "PPN11",
            PpnRate = 11m,
            PphTaxTypeCode = "PPH23",
            PphRate = 2m,
            PpnAmount = 22.00m,
            PphAmount = 4.00m,
            GrandTotal = 218.00m,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _poRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        PurchaseOrder? createdPo = null;
        _poRepo.CreateAsync(Arg.Do<PurchaseOrder>(po => createdPo = po), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (createdPo is null) return null;
                foreach (var item in createdPo.Items)
                    item.BudgetPlanItem = bpi;

                return new PurchaseOrder
                {
                    Id = createdPo.Id,
                    Code = createdPo.Code,
                    VendorShadowId = createdPo.VendorShadowId,
                    Vendor = new VendorShadow { CardCode = "V-01", CardName = "Vendor Alpha" },
                    CreatedBy = new User { Fullname = "Creator" },
                    Status = PurchaseOrderStatus.Draft,
                    DocDate = createdPo.DocDate,
                    CreatedAt = DateTime.UtcNow,
                    Items = createdPo.Items,
                };
            });

        var request = new CreatePurchaseOrderRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();

        await sut.CreateAsync(1, request, TestContext.Current.CancellationToken);

        await _poRepo.Received(1).CreateAsync(
            Arg.Is<PurchaseOrder>(po =>
                po.Code.StartsWith("PO-") &&
                po.Items.Single().PpnTaxTypeCode == "PPN11" &&
                po.Items.Single().PphRate == 2m &&
                po.Items.Single().GrandTotal == 218.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_MixedRfbaItems_RejectsBeforeCreatingPurchaseOrder()
    {
        var items = new List<BudgetPlanItem>
        {
            new()
            {
                Id = 101,
                BudgetPlanId = 5,
                VendorShadowId = 22,
                IsRfba = true,
                ItemShadowId = 11,
                Item = new ItemShadow { Id = 11, ItemCode = "ITEM-A", ItemName = "Item A", AcctCode = "ACC-01", AcctName = "Account" },
                Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
                UomMasterId = 33,
                Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
                BudgetPlan = new BudgetPlan { Id = 5, Code = "BP-001" },
            },
            new()
            {
                Id = 102,
                BudgetPlanId = 5,
                VendorShadowId = 22,
                IsRfba = false,
                ItemShadowId = 12,
                Item = new ItemShadow { Id = 12, ItemCode = "ITEM-B", ItemName = "Item B", AcctCode = "ACC-02", AcctName = "Account" },
                Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
                UomMasterId = 33,
                Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
                BudgetPlan = new BudgetPlan { Id = 5, Code = "BP-001" },
            },
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _poRepo.GetAvailableItemsAsync(
                22,
                Arg.Any<List<long>>(),
                Arg.Any<long?>(),
                Arg.Any<List<long>?>(),
                Arg.Any<CancellationToken>())
            .Returns(items);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder
            {
                Id = 1,
                Code = "PO-001",
                Vendor = new VendorShadow { CardCode = "V-01", CardName = "Vendor Alpha" },
                CreatedBy = new User { Fullname = "Creator" },
                Status = PurchaseOrderStatus.Draft,
                Items = [],
            });

        var act = () => CreateSut().CreateAsync(
            7,
            new CreatePurchaseOrderRequest(22, "remark", DateTime.UtcNow, [101, 102]),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.MixedRfbaItemsNotAllowed);
        await _poRepo.DidNotReceive().CreateAsync(
            Arg.Any<PurchaseOrder>(),
            Arg.Any<CancellationToken>());
        await _codeCounterRepo.DidNotReceive().NextValueAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_MixedRfbaItems_RejectsBeforeUpdatingPurchaseOrder()
    {
        var items = new List<BudgetPlanItem>
        {
            new()
            {
                Id = 101,
                BudgetPlanId = 5,
                VendorShadowId = 22,
                IsRfba = true,
                ItemShadowId = 11,
                Item = new ItemShadow { Id = 11, ItemCode = "ITEM-A", ItemName = "Item A", AcctCode = "ACC-01", AcctName = "Account" },
                Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
                UomMasterId = 33,
                Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
                BudgetPlan = new BudgetPlan { Id = 5, Code = "BP-001" },
            },
            new()
            {
                Id = 102,
                BudgetPlanId = 5,
                VendorShadowId = 22,
                IsRfba = false,
                ItemShadowId = 12,
                Item = new ItemShadow { Id = 12, ItemCode = "ITEM-B", ItemName = "Item B", AcctCode = "ACC-02", AcctName = "Account" },
                Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
                UomMasterId = 33,
                Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
                BudgetPlan = new BudgetPlan { Id = 5, Code = "BP-001" },
            },
        };
        var draft = new PurchaseOrder
        {
            Id = 77,
            VendorShadowId = 22,
            Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
            CreatedBy = new User { Fullname = "Creator" },
            Status = PurchaseOrderStatus.Draft,
            Items = [],
        };

        var readCount = 0;
        _poRepo.GetByIdWithItemsAsync(77, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                readCount++;
                if (readCount == 1) return draft;

                return new PurchaseOrder
                {
                    Id = 77,
                    Code = "PO-0077",
                    VendorShadowId = 22,
                    Vendor = draft.Vendor,
                    CreatedBy = draft.CreatedBy,
                    Status = PurchaseOrderStatus.Draft,
                    Items = items.Select((item, index) => new PurchaseOrderItem
                    {
                        Id = index + 1,
                        BudgetPlanItemId = item.Id,
                        BudgetPlanItem = item,
                        ItemCode = item.Item.ItemCode,
                        ItemName = item.Item.ItemName,
                        CoaCode = item.Item.AcctCode,
                        CoaName = item.Item.AcctName,
                        VendorShadowId = item.VendorShadowId,
                        VendorCode = item.Vendor.CardCode,
                        VendorName = item.Vendor.CardName,
                        UomMasterId = item.UomMasterId,
                        UomCode = item.Uom.Code,
                        UomName = item.Uom.Name,
                        IsRfba = item.IsRfba,
                        CostValue = item.CostValue,
                        Quantity = item.Quantity,
                        TotalValue = item.TotalValue,
                        SortOrder = index + 1,
                        GrandTotal = item.GrandTotal,
                    }).ToList(),
                };
            });
        _poRepo.GetAvailableItemsAsync(
                22,
                Arg.Any<List<long>>(),
                77L,
                Arg.Any<List<long>?>(),
                Arg.Any<CancellationToken>())
            .Returns(items);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var act = () => CreateSut().UpdateAsync(
            77,
            7,
            new UpdatePurchaseOrderRequest("remark", null, [101, 102]),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.MixedRfbaItemsNotAllowed);
        await _poRepo.DidNotReceive().UpdateAsync(
            Arg.Any<PurchaseOrder>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetApprovedBudgetPlansAsync_PassesQueryThroughToRepository()
    {
        var query = new DataTableQuery { Page = 2, Limit = 25, Search = "AC INDO", SortBy = "docDate", SortOrder = "desc" };
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1L, Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetApprovedBudgetPlansWithPoStatusAsync(null, query, Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var sut = CreateSut();
        await sut.GetApprovedBudgetPlansAsync(1L, query, CancellationToken.None);

        await _poRepo.Received(1).GetApprovedBudgetPlansWithPoStatusAsync(null, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecapAsync_NoWarehouseContextAndGlobalAccess_PassesNullWarehouseIdsAndIsRfbaThrough()
    {
        var query = new DataTableQuery { Page = 1, Limit = 20 };
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1L, Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetRecapPurchaseOrdersAsync(true, null, query, Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var sut = CreateSut();
        await sut.GetRecapAsync(true, 1L, query, CancellationToken.None);

        await _poRepo.Received(1).GetRecapPurchaseOrdersAsync(true, null, query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecapAsync_NoWarehouseContextNoGlobalAccess_PassesUserWarehouseIds()
    {
        var query = new DataTableQuery { Page = 1, Limit = 20 };
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1L, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(1L, Arg.Any<CancellationToken>()).Returns(new List<long> { 5L, 6L });
        _poRepo.GetRecapPurchaseOrdersAsync(false, Arg.Any<long[]>(), query, Arg.Any<CancellationToken>())
            .Returns(([], 0));

        var sut = CreateSut();
        await sut.GetRecapAsync(false, 1L, query, CancellationToken.None);

        await _poRepo.Received(1).GetRecapPurchaseOrdersAsync(
            false, Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 5L, 6L })), query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StreamRecapAsync_PassesIsRfbaAndLimitThrough()
    {
        var query = new DataTableQuery { Page = 1, Limit = 20 };
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1L, Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.StreamRecapPurchaseOrdersAsync(true, null, query, 5000, Arg.Any<CancellationToken>())
            .Returns(GetEmptyAsync());

        var sut = CreateSut();
        var result = new List<ApprovedBudgetPlanPoStatusResponse>();
        await foreach (var item in sut.StreamRecapAsync(true, 1L, query, 5000, CancellationToken.None))
            result.Add(item);

        _poRepo.Received(1).StreamRecapPurchaseOrdersAsync(true, null, query, 5000, Arg.Any<CancellationToken>());
        result.Should().BeEmpty();
    }

    private static async IAsyncEnumerable<ApprovedBudgetPlanPoStatusResponse> GetEmptyAsync()
    {
        await Task.CompletedTask;
        yield break;
    }

    [Fact]
    public async Task CreateAsync_copies_CostTreatment_from_budget_item_to_po_item()
    {
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001" };
        var bpi = new BudgetPlanItem
        {
            Id = 100,
            BudgetPlanId = 5L,
            BudgetPlan = bp,
            ItemShadowId = 11,
            Item = new ItemShadow { Id = 11, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "ACC-01", AcctName = "Acct Alpha" },
            VendorShadowId = 22,
            Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
            UomMasterId = 33,
            Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
            CostValue = 100m,
            Quantity = 2m,
            TotalValue = 200m,
            CostTreatment = CostTreatments.Dibiayakan,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _poRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        PurchaseOrder? captured = null;
        _poRepo.CreateAsync(Arg.Do<PurchaseOrder>(po => captured = po), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (captured is null) return null;
                foreach (var item in captured.Items)
                    item.BudgetPlanItem = bpi;

                return new PurchaseOrder
                {
                    Id = captured.Id,
                    Code = captured.Code,
                    VendorShadowId = captured.VendorShadowId,
                    Vendor = new VendorShadow { CardCode = "V-01", CardName = "Vendor Alpha" },
                    CreatedBy = new User { Fullname = "Creator" },
                    Status = PurchaseOrderStatus.Draft,
                    DocDate = captured.DocDate,
                    CreatedAt = DateTime.UtcNow,
                    Items = captured.Items,
                };
            });

        var request = new CreatePurchaseOrderRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();

        await sut.CreateAsync(1, request, TestContext.Current.CancellationToken);

        captured!.Items.Should().ContainSingle()
            .Which.CostTreatment.Should().Be(CostTreatments.Dibiayakan);
    }

    [Fact]
    public async Task CreateAsync_LocksBudgetPlanItemsBeforeCheckingAvailability()
    {
        var bpi = new BudgetPlanItem
        {
            Id = 1L,
            BudgetPlanId = 10L,
            BudgetPlan = new BudgetPlan { Id = 10L, Code = "BP-2026-001" },
            ItemShadowId = 1L,
            Item = new ItemShadow { Id = 1L, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "C", AcctName = "C" },
            VendorShadowId = 1L,
            Vendor = new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" },
            UomMasterId = 1L,
            Uom = new UomMaster { Id = 1L, Code = "PCS", Name = "Pieces" },
            CostValue = 1000m,
            Quantity = 10m,
        };

        _vendorRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" });
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder
            {
                Id = 5L,
                VendorShadowId = 1L,
                Vendor = new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" },
                CreatedBy = new User { Fullname = "Creator" },
                Items = [],
            });

        var sut = CreateSut();
        var request = new CreatePurchaseOrderRequest(1L, "remark", DateTime.UtcNow, [1L]);
        await sut.CreateAsync(7L, request, TestContext.Current.CancellationToken);

        await _poRepo.Received(1).LockBudgetPlanItemsAsync(
            Arg.Is<List<long>>(l => l.SequenceEqual(new List<long> { 1L })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_EmptyDraft_RejectsWithoutCallingSapAndReleasesClaim()
    {
        _poRepo.TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder
            {
                Id = 99L,
                Code = "PO-001",
                VendorShadowId = 1L,
                Status = PurchaseOrderStatus.Draft,
                Items = [],
            });

        var act = () => CreateSut().GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.NoItemsCannotGenerate);
        await _sapClient.DidNotReceiveWithAnyArgs()
            .CreatePurchaseOrderAsync(default!, TestContext.Current.CancellationToken);
        await _poRepo.Received(1).ReleaseGenerationClaimAsync(
            99L, Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task CreateAsync_WithoutItems_CreatesEmptyDraft()
    {
        _vendorRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" });
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>?>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder
            {
                Id = 1L,
                Code = "PO-2608-0001",
                Vendor = new VendorShadow { Id = 1L, CardName = "Vendor One" },
                CreatedBy = new User { Fullname = "Creator" },
                Status = PurchaseOrderStatus.Draft,
                Items = [],
            });

        var result = await CreateSut().CreateAsync(
            7L,
            new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, []),
            TestContext.Current.CancellationToken);

        result.Items.Should().BeEmpty();
        await _poRepo.Received(1).CreateAsync(Arg.Any<PurchaseOrder>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyItems_RejectsRequest()
    {
        _poRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder
            {
                Id = 5L,
                VendorShadowId = 1L,
                Status = PurchaseOrderStatus.Draft,
                Items = [],
            });

        var act = () => CreateSut().UpdateAsync(
            5L,
            7L,
            new UpdatePurchaseOrderRequest(null, null, []),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>()
            .WithMessage(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);
        await _uow.DidNotReceiveWithAnyArgs().ExecuteInTransactionAsync(
            default!,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task CreateAsync_VendorMismatch_ExposesStructuredInvalidItemDetails()
    {
        _vendorRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" });
        _poRepo.GetAvailableItemsAsync(
                1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>?>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _poRepo.GetAvailabilityDiagnosticsAsync(1L, Arg.Any<List<long>>(), Arg.Any<List<long>?>(), Arg.Any<CancellationToken>())
            .Returns([
                new BudgetPlanItemAvailability(
                    135L,
                    Found: true,
                    VendorMatches: false,
                    WarehouseInScope: true,
                    PlanApproved: true,
                    AlreadyGenerated: false,
                    TakenByCode: null,
                    ActualVendorShadowId: 2L)
            ]);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var act = () => CreateSut().CreateAsync(
            7L,
            new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, [135L]),
            TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Code.Should().Be(ErrorCodes.PurchaseOrderItemVendorMismatch);
        ex.Which.Details.Should().BeEquivalentTo(new PurchaseOrderItemValidationDetails(
            [new InvalidPurchaseOrderItem(135L, 1L, 2L)]));
    }

    [Fact]
    public async Task UpdateAsync_WithItems_ExcludesOwnDocumentFromAvailabilityCheck()
    {
        var po = new PurchaseOrder
        {
            Id = 5L,
            VendorShadowId = 1L,
            Status = PurchaseOrderStatus.Draft,
            Items = [],
        };
        _poRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(po);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([]);
        _poRepo.GetAvailabilityDiagnosticsAsync(Arg.Any<long>(), Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = CreateSut();
        var request = new UpdatePurchaseOrderRequest(null, null, [1L]);
        var act = () => sut.UpdateAsync(5L, 7L, request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>(); // empty available list -> item unavailable, proves the call happened
        await _poRepo.Received(1).GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), 5L, Arg.Any<List<long>>(), Arg.Any<CancellationToken>());
        await _poRepo.Received(1).LockBudgetPlanItemsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ItemTakenByAnotherDraftPo_ThrowsSpecificMessage_NotVagueCatchAll()
    {
        var po = new PurchaseOrder
        {
            Id = 5L,
            VendorShadowId = 1L,
            Status = PurchaseOrderStatus.Draft,
            Items = [],
        };
        _poRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(po);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([]);
        // Item exists, vendor matches, warehouse in scope, plan approved, not Generated -- but taken by another Draft PO.
        _poRepo.GetAvailabilityDiagnosticsAsync(Arg.Any<long>(), Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([new BudgetPlanItemAvailability(7L, true, true, true, true, false, "PO-DRAFT-1")]);

        var sut = CreateSut();
        var request = new UpdatePurchaseOrderRequest(null, null, [7L]);
        var act = () => sut.UpdateAsync(5L, 7L, request, TestContext.Current.CancellationToken);

        var ex2 = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex2.Which.Message.Should().Be(ErrorMessages.PurchaseOrder.ItemAlreadyTaken(7L, "PO-DRAFT-1"));
    }

    [Fact]
    public async Task UpdateAsync_LockForEditFails_ThrowsConflictException()
    {
        var po = new PurchaseOrder
        {
            Id = 5L,
            VendorShadowId = 1L,
            Status = PurchaseOrderStatus.Draft,
            Items = [],
        };
        _poRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.LockForEditAsync(5L, Arg.Any<CancellationToken>()).Returns(false);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var request = new UpdatePurchaseOrderRequest(null, null, null);
        var act = () => sut.UpdateAsync(5L, 7L, request, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        ex.Which.Message.Should().Be(ErrorMessages.PurchaseOrder.GenerationInProgress(5L));
    }

    // An explicit warehouse header is validated, but does not narrow a global-access user's
    // unrestricted PO scope.
    [Fact]
    public async Task CreateAsync_GlobalAccessUser_WithWarehouseHeader_PreservesGlobalScope()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(103L);
        _warehouseRepo.GetByIdAsync(103L, Arg.Any<CancellationToken>()).Returns(new WarehouseShadow { Id = 103L });
        _rbacService.HasGlobalAccessAsync(7L, Arg.Any<CancellationToken>()).Returns(true);

        var bpi = new BudgetPlanItem
        {
            Id = 1L,
            BudgetPlanId = 10L,
            BudgetPlan = new BudgetPlan { Id = 10L, Code = "BP-2026-001" },
            ItemShadowId = 1L,
            Item = new ItemShadow { Id = 1L, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "C", AcctName = "C" },
            VendorShadowId = 1L,
            Vendor = new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" },
            UomMasterId = 1L,
            Uom = new UomMaster { Id = 1L, Code = "PCS", Name = "Pieces" },
            CostValue = 1000m,
            Quantity = 10m,
        };

        _vendorRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" });
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new PurchaseOrder
            {
                Id = 5L,
                VendorShadowId = 1L,
                Vendor = new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" },
                CreatedBy = new User { Fullname = "Creator" },
                Items = [],
            });

        var sut = CreateSut();
        var request = new CreatePurchaseOrderRequest(1L, "remark", DateTime.UtcNow, [1L]);
        await sut.CreateAsync(7L, request, TestContext.Current.CancellationToken);

        await _poRepo.Received(1).GetAvailableItemsAsync(
            1L, Arg.Any<List<long>>(), null, null,
            Arg.Any<CancellationToken>());
    }

    // A maker restricted to warehouse 5 (not global access, no explicit warehouse context) must
    // not be able to pull in a budget plan item that lives in a different warehouse (99), even
    // though vendor/plan/generation checks all pass -- and the error must name the real reason,
    // not fall through to the generic "unavailable" catch-all.
    [Fact]
    public async Task CreateAsync_UserRestrictedToOtherWarehouse_ThrowsWarehouseNotAccessibleMessage()
    {
        _warehouseContext.IsSet.Returns(true);
        _warehouseContext.WarehouseId.Returns(5L);
        _warehouseRepo.GetByIdAsync(5L, Arg.Any<CancellationToken>()).Returns(new WarehouseShadow { Id = 5L });
        _rbacService.HasGlobalAccessAsync(3L, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(3L, Arg.Any<CancellationToken>()).Returns(new List<long> { 5L });

        _vendorRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" });
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        // Item belongs to warehouse 99, outside the user's [5L] scope, so the warehouse-scoped
        // availability query returns nothing for it.
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([]);
        _poRepo.GetAvailabilityDiagnosticsAsync(1L, Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([new BudgetPlanItemAvailability(9L, true, true, false, true, false, null)]);

        var sut = CreateSut();
        var request = new CreatePurchaseOrderRequest(1L, "remark", DateTime.UtcNow, [9L]);
        var act = () => sut.CreateAsync(3L, request, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.PurchaseOrder.ItemWarehouseNotAccessible(9L));

        await _poRepo.Received(1).GetAvailableItemsAsync(
            1L, Arg.Any<List<long>>(), null, Arg.Is<List<long>>(l => l.SequenceEqual(new List<long> { 5L })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeleteFails_ThrowsConflictException()
    {
        var po = new PurchaseOrder { Id = 5L, Status = PurchaseOrderStatus.Draft };
        _poRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.SoftDeleteAsync(5L, Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.DeleteAsync(5L, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        ex.Which.Message.Should().Be(ErrorMessages.PurchaseOrder.GenerationInProgress(5L));
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecapDetailAsync_MixedItems_FiltersToMatchingIsRfbaAndSumsOnlyThose()
    {
        var po = new PurchaseOrder
        {
            Id = 1,
            Code = "PO-2607000001",
            Status = PurchaseOrderStatus.Generated,
            DocDate = new DateTime(2026, 7, 1),
            CreatedAt = new DateTime(2026, 6, 29, 10, 21, 0),
            Remark = "Auto-generated PO for BP 149",
            Vendor = new VendorShadow { Id = 3, CardCode = "V.001", CardName = "AC INDO PERKASA" },
            CreatedBy = new User { Id = 9, Fullname = "System Administrator", Email = "a@b.c", CompanyId = 1 },
            GeneratedAt = new DateTime(2026, 7, 3),
            GeneratedBy = new User { Id = 9, Fullname = "System Administrator", Email = "a@b.c", CompanyId = 1 },
            Items =
            [
                new PurchaseOrderItem
                {
                    Id = 10, BudgetPlanItemId = 100, IsRfba = true, ItemCode = "Z.EMKL005", ItemName = "B. Bongkar",
                    CoaCode = "501010211", CoaName = "B. Bongkar", VendorCode = "V.001", VendorName = "AC INDO PERKASA",
                    UomCode = "KG", UomName = "Kilogram", CostValue = 10000, Quantity = 3, TotalValue = 30000,
                    GrandTotal = 33450, SortOrder = 1,
                    BudgetPlanItem = new BudgetPlanItem { Id = 100, BudgetPlanId = 1, BudgetPlan = new BudgetPlan { Id = 1, Code = "BP.2606000094" } },
                },
                new PurchaseOrderItem
                {
                    Id = 11, BudgetPlanItemId = 101, IsRfba = false, ItemCode = "Z.EMKL001", ItemName = "B. Pelayaran",
                    CoaCode = "501010206", CoaName = "B. Pelayaran", VendorCode = "V.001", VendorName = "AC INDO PERKASA",
                    UomCode = "KG", UomName = "Kilogram", CostValue = 20000, Quantity = 100, TotalValue = 2000000,
                    GrandTotal = 2230000, SortOrder = 2,
                    BudgetPlanItem = new BudgetPlanItem { Id = 101, BudgetPlanId = 1, BudgetPlan = new BudgetPlan { Id = 1, Code = "BP.2606000094" } },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.GetPoSummariesByBudgetPlanIdsAsync(Arg.Any<List<long>>(), 1L, Arg.Any<CancellationToken>())
            .Returns(new List<(long, long, string)>());

        var sut = CreateSut();
        var result = await sut.GetRecapDetailAsync(true, 1L, CancellationToken.None);

        result.Items.Should().ContainSingle(i => i.Id == 10);
        result.TotalItems.Should().Be(1);
        result.GrandTotal.Should().Be(33450);
        result.LinkedBudgetPlans.Should().ContainSingle(bp => bp.Code == "BP.2606000094");
        result.Remark.Should().Be("Auto-generated PO for BP 149");
        result.CreatedAt.Should().Be(new DateTime(2026, 6, 29, 10, 21, 0));
    }

    [Fact]
    public async Task GetRecapDetailAsync_NoMatchingItems_ReturnsEmptyItemsAndZeroTotalsNotNotFound()
    {
        var po = new PurchaseOrder
        {
            Id = 2,
            Code = "PO-2607000002",
            Status = PurchaseOrderStatus.Generated,
            DocDate = new DateTime(2026, 7, 1),
            Vendor = new VendorShadow { Id = 3, CardCode = "V.001", CardName = "AC INDO PERKASA" },
            CreatedBy = new User { Id = 9, Fullname = "System Administrator", Email = "a@b.c", CompanyId = 1 },
            Items =
            [
                new PurchaseOrderItem
                {
                    Id = 20, BudgetPlanItemId = 200, IsRfba = false, ItemCode = "Z.EMKL001", ItemName = "B. Pelayaran",
                    CoaCode = "501010206", CoaName = "B. Pelayaran", VendorCode = "V.001", VendorName = "AC INDO PERKASA",
                    UomCode = "KG", UomName = "Kilogram", CostValue = 20000, Quantity = 100, TotalValue = 2000000,
                    GrandTotal = 2230000, SortOrder = 1,
                    BudgetPlanItem = new BudgetPlanItem { Id = 200, BudgetPlanId = 2, BudgetPlan = new BudgetPlan { Id = 2, Code = "BP.2606000095" } },
                },
            ],
        };
        _poRepo.GetByIdWithItemsAsync(2L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.GetPoSummariesByBudgetPlanIdsAsync(Arg.Any<List<long>>(), 2L, Arg.Any<CancellationToken>())
            .Returns(new List<(long, long, string)>());

        var sut = CreateSut();
        var result = await sut.GetRecapDetailAsync(true, 2L, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        result.GrandTotal.Should().Be(0);
        result.LinkedBudgetPlans.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecapDetailAsync_MissingPo_ThrowsNotFoundException()
    {
        _poRepo.GetByIdWithItemsAsync(999L, Arg.Any<CancellationToken>()).Returns((PurchaseOrder?)null);

        var sut = CreateSut();
        var act = async () => await sut.GetRecapDetailAsync(true, 999L, CancellationToken.None);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.NotFoundException>();
    }

    private static PurchaseOrder CreateGeneratablePo()
    {
        var warehouse = new WarehouseShadow { Code = "WH-01" };
        var bp = new BudgetPlan { Id = 10L, Code = "BP-2026-001", Warehouse = warehouse };
        var bpi = new BudgetPlanItem { Id = 1L, BudgetPlanId = 10L, BudgetPlan = bp };
        var poItem = new PurchaseOrderItem
        {
            Id = 1L, BudgetPlanItemId = 1L, BudgetPlanItem = bpi, PurchaseOrderId = 99L,
            ItemShadowId = 1L, ItemCode = "ITEM-A", ItemName = "Item Alpha", CoaCode = "C", CoaName = "C",
            VendorShadowId = 1L, VendorCode = "V-001", VendorName = "Vendor One",
            UomMasterId = 1L, UomCode = "PCS", UomName = "Pieces",
            CostValue = 1000m, Quantity = 10m, TotalValue = 10000m, SortOrder = 1,
            PpnTaxTypeCode = "PPN11",
        };
        return new PurchaseOrder
        {
            Id = 99L, Code = "PO-001", VendorShadowId = 1L, CompanyId = 1L,
            Company = new Company { Id = 1L, Code = "Test", Name = "Test Co" },
            Vendor = new VendorShadow { CardCode = "V-001", CardName = "Vendor One" },
            CreatedBy = new User { Fullname = "Creator" },
            Status = PurchaseOrderStatus.Draft,
            DocDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow,
            Items = [poItem],
        };
    }

    [Fact]
    public async Task GenerateAsync_MixedRfbaItems_RejectsBeforeCallingSapAndReleasesClaim()
    {
        var po = CreateGeneratablePo();
        var firstItem = po.Items.First();
        po.Items.Add(new PurchaseOrderItem
        {
            Id = 2L,
            PurchaseOrderId = po.Id,
            BudgetPlanItemId = 2L,
            BudgetPlanItem = firstItem.BudgetPlanItem,
            IsRfba = true,
            ItemCode = "ITEM-B",
            ItemName = "Item Beta",
            VendorCode = "V-001",
            VendorName = "Vendor One",
            Quantity = 1m,
            CostValue = 100m,
            TotalValue = 100m,
            SortOrder = 2,
        });
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var act = () => CreateSut().GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage(ErrorMessages.PurchaseOrder.MixedRfbaItemsNotAllowed);
        await _sapClient.DidNotReceive().CreatePurchaseOrderAsync(
            Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>());
        await _poRepo.Received(1).ReleaseGenerationClaimAsync(
            99L, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SapSucceeds_PersistsSapPoNumberAndDocEntry()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreatePoResult("9001", 501));
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _userRepo.GetByIdAsync(7L, Arg.Any<CancellationToken>()).Returns(new User { Fullname = "Generator" });

        var sut = CreateSut();
        var result = await sut.GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        result.SapPoNumber.Should().Be("9001");
        po.SapDocEntry.Should().Be(501);
        po.Status.Should().Be(PurchaseOrderStatus.Generated);
        await _poRepo.Received(1).MarkGeneratedAsync(99L, Arg.Any<string>(), "9001", 501, 7L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_PassesWarehouseCodeToSapRequest()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreatePoResult("9001", 501));
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _userRepo.GetByIdAsync(7L, Arg.Any<CancellationToken>()).Returns(new User { Fullname = "Generator" });

        var sut = CreateSut();
        await sut.GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        await _sapClient.Received(1).CreatePurchaseOrderAsync(
            Arg.Is<SapCreatePoRequest>(r =>
                r.Items.Single().WarehouseCode == "WH-01" &&
                r.Items.Single().TaxCode == "PPN11"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SapReturnsNull_ThrowsValidationExceptionAndDoesNotMarkGenerated()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns((SapCreatePoResult?)null);

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(99L, 7L);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _poRepo.DidNotReceive().MarkGeneratedAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SapRejects_LeavesStandaloneDraftAlone()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns<SapCreatePoResult?>(_ => throw new WAMS.Domain.Exceptions.ValidationException("SAP rejected PO-001"));

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(99L, 7L);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _poRepo.DidNotReceive().SoftDeleteAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAndGenerateAsync_SapRejects_SoftDeletesTheDraftItJustCreated()
    {
        var po = CreateGeneratablePo();
        var bpi = new BudgetPlanItem
        {
            Id = 1L,
            BudgetPlanId = 10L,
            BudgetPlan = new BudgetPlan { Id = 10L, Code = "BP-2026-001" },
            ItemShadowId = 1L,
            Item = new ItemShadow { Id = 1L, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "C", AcctName = "C" },
            VendorShadowId = 1L,
            Vendor = new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" },
            UomMasterId = 1L,
            Uom = new UomMaster { Id = 1L, Code = "PCS", Name = "Pieces" },
            CostValue = 1000m,
            Quantity = 10m,
        };

        _vendorRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 1L, CardCode = "V-001", CardName = "Vendor One" });
        _poRepo.GetAvailableItemsAsync(1L, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.GetByIdWithItemsAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns<SapCreatePoResult?>(_ => throw new WAMS.Domain.Exceptions.ValidationException("SAP rejected PO-001"));

        var request = new CreatePurchaseOrderRequest(1L, null, DateTime.UtcNow, [1L]);
        var sut = CreateSut();
        var act = () => sut.CreateAndGenerateAsync(7L, request, CancellationToken.None);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _poRepo.Received(1).SoftDeleteAsync(99L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ClaimAlreadyHeld_ThrowsConflictExceptionAndSkipsSap()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        await _sapClient.DidNotReceive().CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ClaimAcquired_ClaimsBeforeCallingSap()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreatePoResult("9001", 501));
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _userRepo.GetByIdAsync(7L, Arg.Any<CancellationToken>()).Returns(new User { Fullname = "Generator" });

        var sut = CreateSut();
        await sut.GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        await _poRepo.Received(1).TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _poRepo.DidNotReceive().ReleaseGenerationClaimAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SapThrowsAfterClaim_ReleasesClaimAndRethrows()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns<SapCreatePoResult?>(_ => throw new WAMS.Domain.Exceptions.ValidationException("SAP rejected"));

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _poRepo.Received(1).ReleaseGenerationClaimAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_MarkGeneratedFails_ThrowsConflictExceptionAndReleasesClaim()
    {
        var po = CreateGeneratablePo();
        _poRepo.GetByIdWithItemsAsync(99L, Arg.Any<CancellationToken>()).Returns(po);
        _poRepo.TryClaimForGenerationAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreatePurchaseOrderAsync(Arg.Any<SapCreatePoRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreatePoResult("9001", 501));
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        _poRepo.MarkGeneratedAsync(99L, Arg.Any<string>(), "9001", 501, 7L, Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(99L, 7L, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        ex.Which.Message.Should().Be(ErrorMessages.PurchaseOrder.GenerationInProgress(99L));
        await _poRepo.Received(1).ReleaseGenerationClaimAsync(99L, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
