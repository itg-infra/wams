namespace WAMS.Application.Tests.Services;

using FluentAssertions;
using NSubstitute;
using WAMS.Application.DTOs.AccountPayables;
using WAMS.Application.Interfaces.AccountPayables;
using WAMS.Application.Interfaces.Common;
using WAMS.Application.Interfaces.PurchaseOrders;
using WAMS.Application.Interfaces.Rbac;
using WAMS.Application.Interfaces.Users;
using WAMS.Application.Interfaces.Vendors;
using WAMS.Application.Interfaces.Warehouses;
using WAMS.Application.Services.AccountPayables;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using Xunit;

public class AccountPayableServiceTests
{
    private readonly IAccountPayableRepository _apRepo = Substitute.For<IAccountPayableRepository>();
    private readonly IVendorShadowRepository _vendorRepo = Substitute.For<IVendorShadowRepository>();
    private readonly IPurchaseOrderRepository _poRepo = Substitute.For<IPurchaseOrderRepository>();
    private readonly ISapApiClient _sapClient = Substitute.For<ISapApiClient>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IWarehouseContext _warehouseContext = Substitute.For<IWarehouseContext>();
    private readonly IWarehouseShadowRepository _warehouseRepo = Substitute.For<IWarehouseShadowRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IRbacService _rbacService = Substitute.For<IRbacService>();
    private readonly ICodeCounterRepository _codeCounterRepo = Substitute.For<ICodeCounterRepository>();

    public AccountPayableServiceTests()
    {
        _apRepo.LockForEditAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _apRepo.SoftDeleteAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _apRepo.MarkGeneratedAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<int?>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, (int SapDocEntry, int LineIndex)>());

        // Default every test to "global access, no warehouse restriction" (null warehouseIds) unless
        // a test explicitly overrides warehouseContext/rbacService to exercise the restricted path.
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
    }

    private AccountPayableService CreateSut() => new(
        _apRepo, _vendorRepo, _poRepo, _sapClient, _uow, _warehouseContext, _warehouseRepo, _userRepo, _rbacService, _codeCounterRepo);

    [Fact]
    public async Task CreateAsync_WithoutItems_RejectsRequestBeforeLookupOrPersistence()
    {
        var request = new CreateAccountPayableRequest(22L, null, DateTime.UtcNow, []);

        var act = () => CreateSut().CreateAsync(1L, request, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);
        await _vendorRepo.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyItems_RejectsRequestWithoutClearingDraft()
    {
        var ap = new AccountPayable
        {
            Id = 5L,
            VendorShadowId = 22L,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
        };
        ap.Items.Add(new AccountPayableItem { BudgetPlanItemId = 100L, BudgetPlanTotal = 200m });
        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(ap);

        var request = new UpdateAccountPayableRequest(null, null, [], null);
        var act = () => CreateSut().UpdateAsync(5L, 1L, request, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.Validation.Common.AtLeastOneLineItemRequired);
        ap.Items.Should().ContainSingle();
        await _uow.DidNotReceive().ExecuteInTransactionAsync(
            Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableItemsByBudgetPlansAsync_DefaultParam_CallsRepoWithIncludeGeneratedFalse()
    {
        var expected = new List<AvailableApItemResponse>();
        _apRepo.GetAvailableItemsByBudgetPlansAsync(1L, Arg.Any<List<long>>(), false, Arg.Any<long?>(), null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var result = await sut.GetAvailableItemsByBudgetPlansAsync(9L, 1L, [], ct: ct);

        await _apRepo.Received(1).GetAvailableItemsByBudgetPlansAsync(
            1L, Arg.Any<List<long>>(), false, Arg.Any<long?>(), null, Arg.Any<CancellationToken>());
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetAvailableItemsByBudgetPlansAsync_IncludeGeneratedTrue_CallsRepoWithTrue()
    {
        var item = new AvailableApItemResponse(1, 10, "BP-001", null, 2L, "V-01", "Vendor Alpha",
            "ITEM-A", "Item Alpha", "ACC-01", "Acct Alpha", "PCS", "Pieces",
            false, null, 100m, 2m, 200m, true, "AP-999");
        var expected = new List<AvailableApItemResponse> { item };
        _apRepo.GetAvailableItemsByBudgetPlansAsync(1L, Arg.Any<List<long>>(), true, Arg.Any<long?>(), null, Arg.Any<CancellationToken>())
            .Returns(expected);

        var ct = TestContext.Current.CancellationToken;
        var sut = CreateSut();
        var result = await sut.GetAvailableItemsByBudgetPlansAsync(9L, 1L, [], includeGenerated: true, ct: ct);

        await _apRepo.Received(1).GetAvailableItemsByBudgetPlansAsync(
            1L, Arg.Any<List<long>>(), true, Arg.Any<long?>(), null, Arg.Any<CancellationToken>());
        result.Should().HaveCount(1);
        result[0].IsGenerated.Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableItemsByBudgetPlansAsync_ExcludeIdNotDraft_ThrowsValidationException()
    {
        var other = new AccountPayable { Id = 5L, VendorShadowId = 22, Status = WAMS.Domain.Enums.AccountPayableStatus.Generated };
        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(other);

        var sut = CreateSut();
        var act = () => sut.GetAvailableItemsByBudgetPlansAsync(
            9L, 22L, [], excludeAccountPayableId: 5L, ct: TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.AccountPayable.CannotUpdateOnlyDraft);
    }

    [Fact]
    public async Task GetAvailableItemsByBudgetPlansAsync_ExcludeIdVendorMismatch_ThrowsValidationException()
    {
        var other = new AccountPayable { Id = 5L, VendorShadowId = 2L, Status = WAMS.Domain.Enums.AccountPayableStatus.Draft };
        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(other);

        var sut = CreateSut();
        var act = () => sut.GetAvailableItemsByBudgetPlansAsync(
            9L, 22L, [], excludeAccountPayableId: 5L, ct: TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.AccountPayable.ItemVendorMismatch(22L));
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
            CostTreatment = CostTreatments.Dibiayakan,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Test User", Email = "test@example.com" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });

        AccountPayable? captured = null;
        _apRepo.CreateAsync(Arg.Do<AccountPayable>(ap => captured = ap), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        await sut.CreateAsync(1, request, ct);

        captured.Should().NotBeNull();
        captured!.Code.Should().StartWith("AP-");
        var item = captured!.Items.Should().ContainSingle().Subject;
        item.PpnTaxTypeCode.Should().Be("PPN11");
        item.PpnRate.Should().Be(11m);
        item.PphTaxTypeCode.Should().Be("PPH23");
        item.PphRate.Should().Be(2m);
        item.PpnAmount.Should().Be(22.00m);
        item.PphAmount.Should().Be(4.00m);
        item.GrandTotal.Should().Be(218.00m);
        item.CostTreatment.Should().Be(CostTreatments.Dibiayakan);
    }

    [Fact]
    public async Task CreateAsync_LocksBudgetPlanItemsBeforeCheckingAvailability()
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
            GrandTotal = 200m,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        await sut.CreateAsync(1, request, ct);

        await _apRepo.Received(1).LockBudgetPlanItemsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DiscountExceedsDpp_ThrowsValidationException()
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
            GrandTotal = 200m,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100], DiscountAmount: 201m);

        var act = () => sut.CreateAsync(1L, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _apRepo.DidNotReceive().CreateAsync(Arg.Any<AccountPayable>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DiscountWithinBounds_PersistsDiscountAmount()
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
            GrandTotal = 200m,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Test User" });

        AccountPayable? captured = null;
        _apRepo.CreateAsync(Arg.Do<AccountPayable>(a => captured = a), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100], DiscountAmount: 50m);

        await sut.CreateAsync(1L, request, ct);

        captured.Should().NotBeNull();
        captured!.DiscountAmount.Should().Be(50m);
    }

    [Fact]
    public async Task UpdateAsync_DiscountExceedsDpp_ThrowsValidationException()
    {
        var ap = new AccountPayable
        {
            Id = 1,
            VendorShadowId = 22,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
        };
        ap.Items.Add(new AccountPayableItem { BudgetPlanTotal = 100m });

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var request = new UpdateAccountPayableRequest(null, null, null, DiscountAmount: 150m);

        var act = () => sut.UpdateAsync(1L, 1L, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithItems_ExcludesOwnDocumentFromAvailabilityCheck()
    {
        var ap = new AccountPayable
        {
            Id = 5L,
            VendorShadowId = 22,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
        };

        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), 5L, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _apRepo.GetAvailabilityDiagnosticsAsync(Arg.Any<long>(), Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var request = new UpdateAccountPayableRequest(null, null, [1L], null);

        var act = () => sut.UpdateAsync(5L, 1L, request);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _apRepo.Received(1).GetAvailableItemsAsync(Arg.Any<long>(), Arg.Any<List<long>>(), 5L, Arg.Any<List<long>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ItemTakenByAnotherDraftAp_ThrowsSpecificMessage_NotVagueCatchAll()
    {
        var ap = new AccountPayable
        {
            Id = 5L,
            VendorShadowId = 22,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
        };

        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), 5L, Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        // Item exists, vendor matches, recap approved, not Generated -- but taken by another Draft AP.
        _apRepo.GetAvailabilityDiagnosticsAsync(Arg.Any<long>(), Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([new BudgetPlanItemAvailability(7L, true, true, true, true, false, "AP-DRAFT-1")]);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var request = new UpdateAccountPayableRequest(null, null, [7L], null);

        var act = () => sut.UpdateAsync(5L, 1L, request);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Be(ErrorMessages.AccountPayable.ItemAlreadyTaken(7L, "AP-DRAFT-1"));
    }

    [Fact]
    public async Task UpdateAsync_LockForEditFails_ThrowsConflictException()
    {
        var ap = new AccountPayable
        {
            Id = 5L,
            VendorShadowId = 22,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
        };
        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.LockForEditAsync(5L, Arg.Any<CancellationToken>()).Returns(false);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var sut = CreateSut();
        var request = new UpdateAccountPayableRequest(null, null, null, null);
        var act = () => sut.UpdateAsync(5L, 1L, request, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        ex.Which.Message.Should().Be(ErrorMessages.AccountPayable.GenerationInProgress(5L));
    }

    // Pins the null-means-unrestricted convention end to end: a global-access user must see
    // `warehouseIds: null` reach the availability query, not an empty list (which would mean
    // "match nothing" and silently break every super-admin/global-access role).
    [Fact]
    public async Task CreateAsync_GlobalAccessUser_PassesNullWarehouseIdsToAvailabilityQuery()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(1L, Arg.Any<CancellationToken>()).Returns(true);

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
            GrandTotal = 200m,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        await sut.CreateAsync(1L, request, ct);

        await _apRepo.Received(1).GetAvailableItemsAsync(
            22, Arg.Any<List<long>>(), null, null, Arg.Any<CancellationToken>());
    }

    // A maker restricted to warehouse 5 (not global access, no explicit warehouse context) must
    // not be able to pull in a budget plan item that lives in a different warehouse, even though
    // vendor/recap/generation checks all pass -- and the error must name the real reason, not
    // fall through to the generic "unavailable" catch-all.
    [Fact]
    public async Task CreateAsync_UserRestrictedToOtherWarehouse_ThrowsWarehouseNotAccessibleMessage()
    {
        _warehouseContext.IsSet.Returns(false);
        _rbacService.HasGlobalAccessAsync(3L, Arg.Any<CancellationToken>()).Returns(false);
        _userRepo.GetUserWarehouseIdsAsync(3L, Arg.Any<CancellationToken>()).Returns(new List<long> { 5L });

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));
        // Item belongs to a warehouse outside the user's [5L] scope, so the warehouse-scoped
        // availability query returns nothing for it.
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _apRepo.GetAvailabilityDiagnosticsAsync(22, Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([new BudgetPlanItemAvailability(9L, true, true, false, true, false, null)]);

        var sut = CreateSut();
        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [9L]);
        var act = () => sut.CreateAsync(3L, request, TestContext.Current.CancellationToken);

        var ex2 = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex2.Which.Message.Should().Be(ErrorMessages.AccountPayable.ItemWarehouseNotAccessible(9L));

        await _apRepo.Received(1).GetAvailableItemsAsync(
            22, Arg.Any<List<long>>(), null, Arg.Is<List<long>>(l => l.SequenceEqual(new List<long> { 5L })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeleteFails_ThrowsConflictException()
    {
        var ap = new AccountPayable { Id = 5L, Status = WAMS.Domain.Enums.AccountPayableStatus.Draft };
        _apRepo.GetByIdWithItemsAsync(5L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.SoftDeleteAsync(5L, Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.DeleteAsync(5L, TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        ex.Which.Message.Should().Be(ErrorMessages.AccountPayable.GenerationInProgress(5L));
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_WithDiscount_ReturnsComputedTotals()
    {
        var vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" };
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001" };
        var bpi = new BudgetPlanItem { Id = 100, BudgetPlanId = 5L, BudgetPlan = bp };
        var ap = new AccountPayable
        {
            Id = 1,
            Code = "AP-2607000001",
            VendorShadowId = 22,
            Vendor = vendor,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
            DiscountAmount = 40m,
            CreatedBy = new User { Id = 1L, Fullname = "Test User" },
        };
        ap.Items.Add(new AccountPayableItem
        {
            BudgetPlanItem = bpi,
            BudgetPlanTotal = 400m,
            PpnAmount = 44m,
            PphAmount = 8m,
            GrandTotal = 452m,
            BudgetRealization = 400m,
            BudgetVariance = 0m,
        });

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var result = await sut.GetByIdAsync(1L, ct);

        result.DiscountAmount.Should().Be(40m);
        result.DiscountPercent.Should().Be(10m);
        result.TotalRealization.Should().Be(400m);
        result.TotalVariance.Should().Be(-40m);
        result.TaxInclusiveGrandTotal.Should().Be(412m);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsLinkedBudgetPlanIdsAndCodes()
    {
        var bp = new BudgetPlan { Id = 206L, Code = "BP-2608000008" };
        var bpi = new BudgetPlanItem { Id = 663L, BudgetPlanId = 206L, BudgetPlan = bp };
        var ap = new AccountPayable
        {
            Id = 40L,
            Code = "AP-2608000005",
            VendorShadowId = 22L,
            Vendor = new VendorShadow { Id = 22L, CardCode = "V-01", CardName = "Vendor Alpha" },
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
            CreatedBy = new User { Id = 1L, Fullname = "Test User" },
        };
        ap.Items.Add(new AccountPayableItem { BudgetPlanItemId = 663L, BudgetPlanItem = bpi });

        _apRepo.GetByIdWithItemsAsync(40L, Arg.Any<CancellationToken>()).Returns(ap);

        var result = await CreateSut().GetByIdAsync(40L, TestContext.Current.CancellationToken);

        result.LinkedBudgetPlans.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Id = 206L, Code = "BP-2608000008" });
        result.LinkedBudgetPlanCodes.Should().ContainSingle()
            .Which.Should().Be("BP-2608000008");
        result.Items.Should().ContainSingle().Which.BudgetPlanId.Should().Be(206L);
    }

    [Fact]
    public async Task CreateAsync_ResponseIncludesTaxTotals()
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
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });
        _apRepo.CreateAsync(Arg.Any<AccountPayable>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        var result = await sut.CreateAsync(1, request, ct);

        result.TotalPpnAmount.Should().Be(22.00m);
        result.TotalPphAmount.Should().Be(4.00m);
        result.TaxInclusiveGrandTotal.Should().Be(218.00m);
        result.Items.Should().ContainSingle().Which.GrandTotal.Should().Be(218.00m);
    }

    [Fact]
    public async Task CreateAsync_MixedIsRfbaAcrossItems_Succeeds()
    {
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001" };
        BudgetPlanItem MakeItem(long id, bool isRfba) => new()
        {
            Id = id,
            BudgetPlanId = 5L,
            BudgetPlan = bp,
            ItemShadowId = 11,
            Item = new ItemShadow { Id = 11, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "ACC-01", AcctName = "Acct Alpha" },
            VendorShadowId = 22,
            Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
            UomMasterId = 33,
            Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
            CostValue = 100m,
            Quantity = 1m,
            TotalValue = 100m,
            IsRfba = isRfba,
            GrandTotal = 100m,
        };
        var rfbaItem = MakeItem(100, true);
        var nonRfbaItem = MakeItem(101, false);

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([rfbaItem, nonRfbaItem]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);

        AccountPayable? captured = null;
        _apRepo.CreateAsync(Arg.Do<AccountPayable>(ap => captured = ap), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100, 101]);
        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;

        await sut.CreateAsync(1, request, ct);

        captured.Should().NotBeNull();
        captured!.Items.Should().HaveCount(2);
        captured.Items.Should().Contain(i => i.BudgetPlanItemId == 100 && i.IsRfba);
        captured.Items.Should().Contain(i => i.BudgetPlanItemId == 101 && !i.IsRfba);
    }

    private static AccountPayable BuildGeneratableAp(params AccountPayableItem[] items)
    {
        var ap = new AccountPayable
        {
            Id = 1L,
            Code = "AP-2607000001",
            VendorShadowId = 22,
            DocDate = new DateTime(2026, 7, 12, 0, 0, 0, DateTimeKind.Utc),
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
            Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
            CreatedBy = new User { Id = 1L, Fullname = "Tester" },
        };
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001", Warehouse = new WarehouseShadow { Id = 1L, Code = "WH-01", Name = "Warehouse Alpha" } };
        foreach (var item in items)
        {
            item.BudgetPlanItem = new BudgetPlanItem { Id = item.BudgetPlanItemId, BudgetPlanId = bp.Id, BudgetPlan = bp };
            ap.Items.Add(item);
        }
        return ap;
    }

    [Fact]
    public async Task GenerateAsync_NonRfba_BuildsAccumulatedWhTaxAndCallsInvoiceOnly()
    {
        var item1 = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "ITEM-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 2m, BudgetPlanTotal = 200m,
            IsRfba = false,
            PphTaxTypeCode = "PPH23", PphRate = 2m, PphAmount = 4m,
        };
        var item2 = new AccountPayableItem
        {
            Id = 2, VendorCode = "V-01", ItemCode = "ITEM-B", CoaCode = "ACC-02", UomCode = "PCS",
            UnitCost = 50m, UnitCount = 1m, BudgetPlanTotal = 50m,
            IsRfba = false,
            PphTaxTypeCode = "PPH23", PphRate = 2m, PphAmount = 1m,
        };
        var item3 = new AccountPayableItem
        {
            Id = 3, VendorCode = "V-01", ItemCode = "ITEM-C", CoaCode = "ACC-03", UomCode = "PCS",
            UnitCost = 30m, UnitCount = 1m, BudgetPlanTotal = 30m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item1, item2, item3);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });

        SapCreateApInvoiceRequest? captured = null;
        _sapClient.CreateApInvoiceAsync(Arg.Do<SapCreateApInvoiceRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9101", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var result = await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        captured.Should().NotBeNull();
        captured!.WhTax.Should().ContainSingle(w => w.WtCode == "PPH23" && w.TaxableAmount == 250m);
        captured.ApdpDocEntry.Should().BeNull();
        captured.DrawAmount.Should().BeNull();
        captured.Items.First(i => i.ItemCode == "ITEM-A").PphTaxTypeCode.Should().Be("PPH23");
        captured.Items.First(i => i.ItemCode == "ITEM-B").PphTaxTypeCode.Should().Be("PPH23");
        captured.Items.First(i => i.ItemCode == "ITEM-C").PphTaxTypeCode.Should().BeNull();
        await _sapClient.DidNotReceive().CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
        result.SapApNumber.Should().Be("9101");
    }

    [Fact]
    public async Task GenerateAsync_Rfba_CallsApdpThenInvoiceWithTapdpAndNoWhTax()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "ITEM-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 2m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = true,
            PphTaxTypeCode = "PPH23", PphRate = 2m, PphAmount = 4m,
        };
        var ap = BuildGeneratableAp(item);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApdpResult(301));

        SapCreateApInvoiceRequest? captured = null;
        _sapClient.CreateApInvoiceAsync(Arg.Do<SapCreateApInvoiceRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9102", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.Received(1).CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
        captured.Should().NotBeNull();
        captured!.ApdpDocEntry.Should().Be(301);
        captured.DrawAmount.Should().Be(200m);
        captured.WhTax.Should().BeNull();
        ap.SapApdpDocEntry.Should().Be(301);
    }

    [Fact]
    public async Task GenerateAsync_Rfba_ApdpAlreadySet_SkipsApdpCallOnRetry()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "ITEM-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 2m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = true,
        };
        var ap = BuildGeneratableAp(item);
        ap.SapApdpDocEntry = 301; // simulates a prior partial failure: APDP succeeded, invoice call didn't

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9103", 402));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.DidNotReceive().CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>());
        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r => r.ApdpDocEntry == 301), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_NonRfbaWithDiscount_AppliesSameDiscountPercentToAllLines()
    {
        var item1 = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 300m, UnitCount = 1m, BudgetPlanTotal = 300m, GrandTotal = 300m,
            IsRfba = false,
        };
        var item2 = new AccountPayableItem
        {
            Id = 2, VendorCode = "V-01", ItemCode = "B", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 200m, UnitCount = 1m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item1, item2);
        ap.DiscountAmount = 50m;

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("SAP-AP-001", 999));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r =>
                r.Items.All(i => i.DiscountPercent == 10m)), // 50 / 500 * 100
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ZeroDiscount_LeavesDiscountPercentNull()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 300m, UnitCount = 1m, BudgetPlanTotal = 300m, GrandTotal = 300m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("SAP-AP-001", 999));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r => r.Items.All(i => i.DiscountPercent == null)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_RfbaWithDiscount_DrawAmountNetsOutDiscount()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 500m, UnitCount = 1m, BudgetPlanTotal = 500m, GrandTotal = 500m,
            IsRfba = true,
        };
        var ap = BuildGeneratableAp(item);
        ap.DiscountAmount = 50m;
        ap.SapApdpDocEntry = 777; // APDP already created, skip straight to invoice/draw

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("SAP-AP-001", 999));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r => r.DrawAmount == 450m), // 500 - 50
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_RfbaWithDiscount_ApdpLinesCarryDiscountPercent()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 500m, UnitCount = 1m, BudgetPlanTotal = 500m, GrandTotal = 500m,
            IsRfba = true,
        };
        var ap = BuildGeneratableAp(item);
        ap.DiscountAmount = 50m; // SapApdpDocEntry left null so the APDP branch actually runs

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApdpResult(301));
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9104", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.Received(1).CreateApDownPaymentAsync(
            Arg.Is<SapCreateApdpRequest>(r => r.Items.All(i => i.DiscountPercent == 10m)), // 50 / 500 * 100
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_Mixed_ApdpContainsOnlyRfbaItems()
    {
        var rfbaItem = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "RFBA-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 200m, UnitCount = 1m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = true,
        };
        var nonRfbaItem = new AccountPayableItem
        {
            Id = 2, VendorCode = "V-01", ItemCode = "NONRFBA-B", CoaCode = "ACC-02", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 1m, BudgetPlanTotal = 100m, GrandTotal = 100m,
            IsRfba = false, PphTaxTypeCode = "PPH23", PphRate = 2m, PphAmount = 2m,
        };
        var ap = BuildGeneratableAp(rfbaItem, nonRfbaItem);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApDownPaymentAsync(Arg.Any<SapCreateApdpRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApdpResult(555));
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9105", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await _sapClient.Received(1).CreateApDownPaymentAsync(
            Arg.Is<SapCreateApdpRequest>(r => r.Items.Count == 1 && r.Items[0].ItemCode == "RFBA-A"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_Mixed_DrawAmountIsRfbaSubsetNetOfItsProportionalDiscountShare()
    {
        var rfbaItem = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "RFBA-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 300m, UnitCount = 1m, BudgetPlanTotal = 300m, GrandTotal = 300m,
            IsRfba = true,
        };
        var nonRfbaItem = new AccountPayableItem
        {
            Id = 2, VendorCode = "V-01", ItemCode = "NONRFBA-B", CoaCode = "ACC-02", UomCode = "PCS",
            UnitCost = 200m, UnitCount = 1m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(rfbaItem, nonRfbaItem);
        ap.DiscountAmount = 50m; // DppTotal = 500, RFBA share = 50 * 300/500 = 30
        ap.SapApdpDocEntry = 555; // already created, skip straight to invoice/draw

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9106", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        // DrawAmount = 300 - 30 = 270 (RFBA subset, net of its proportional discount share -
        // PM confirmed discount applies to both RFBA and non-RFBA when mixed)
        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r => r.DrawAmount == 270m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_Mixed_WhTaxIsNullEvenThoughNonRfbaLineHasPphType()
    {
        var rfbaItem = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "RFBA-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 200m, UnitCount = 1m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = true,
        };
        var nonRfbaItem = new AccountPayableItem
        {
            Id = 2, VendorCode = "V-01", ItemCode = "NONRFBA-B", CoaCode = "ACC-02", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 1m, BudgetPlanTotal = 100m, GrandTotal = 100m,
            IsRfba = false, PphTaxTypeCode = "PPH23", PphRate = 2m, PphAmount = 2m,
        };
        var ap = BuildGeneratableAp(rfbaItem, nonRfbaItem);
        ap.SapApdpDocEntry = 555;

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9107", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        // Any RFBA presence suppresses WHT for the whole document, per client's stated rule
        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r => r.WhTax == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_Mixed_DiscountPercentAppliesUniformlyToBothGroups()
    {
        var rfbaItem = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "RFBA-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 300m, UnitCount = 1m, BudgetPlanTotal = 300m, GrandTotal = 300m,
            IsRfba = true,
        };
        var nonRfbaItem = new AccountPayableItem
        {
            Id = 2, VendorCode = "V-01", ItemCode = "NONRFBA-B", CoaCode = "ACC-02", UomCode = "PCS",
            UnitCost = 200m, UnitCount = 1m, BudgetPlanTotal = 200m, GrandTotal = 200m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(rfbaItem, nonRfbaItem);
        ap.DiscountAmount = 50m; // 50 / 500 (whole document DPP) * 100 = 10%, same for every line
        ap.SapApdpDocEntry = 555;

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9108", 401));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        await sut.GenerateAsync(1L, userId: 1L, ct: ct);

        // PM confirmed discount "should include" RFBA when mixed - same discountPercent
        // applies to every line, RFBA and non-RFBA alike (not split by group).
        await _sapClient.Received(1).CreateApInvoiceAsync(
            Arg.Is<SapCreateApInvoiceRequest>(r => r.Items.All(i => i.DiscountPercent == 10m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ClaimAlreadyHeld_ThrowsConflictExceptionAndSkipsSap()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 1m, BudgetPlanTotal = 100m, GrandTotal = 100m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var act = () => sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        await _sapClient.DidNotReceive().CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SapThrowsAfterClaim_ReleasesClaimAndRethrows()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 1m, BudgetPlanTotal = 100m, GrandTotal = 100m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns<SapCreateApInvoiceResult?>(_ => throw new WAMS.Domain.Exceptions.ValidationException("SAP rejected"));

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var act = () => sut.GenerateAsync(1L, userId: 1L, ct: ct);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _apRepo.Received(1).ReleaseGenerationClaimAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_MarkGeneratedFails_ThrowsConflictExceptionAndReleasesClaim()
    {
        var item = new AccountPayableItem
        {
            Id = 1, VendorCode = "V-01", ItemCode = "A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 1m, BudgetPlanTotal = 100m, GrandTotal = 100m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ci.Arg<List<long>>();
                return ids.Distinct().ToDictionary(id => id, id => (999, 0));
            });
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new SapCreateApInvoiceResult("9101", 401));
        _apRepo.MarkGeneratedAsync(1L, Arg.Any<string>(), "9101", 401, Arg.Any<int?>(), 1L, Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(1L, userId: 1L, ct: TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ConflictException>();
        ex.Which.Message.Should().Be(ErrorMessages.AccountPayable.GenerationInProgress(1L));
        await _apRepo.Received(1).ReleaseGenerationClaimAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewAsync_ReturnsComputedTotals_WithoutPersisting()
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
            CostValue = 200m,
            Quantity = 2m,
            TotalValue = 400m,
            PpnRate = 11m,
            PphRate = 2m,
            PpnAmount = 44m,
            PphAmount = 8m,
            GrandTotal = 452m,
        };

        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var request = new PreviewAccountPayableRequest(22, [100], DiscountAmount: 40m);

        var result = await sut.PreviewAsync(1L, request, ct);

        result.DppTotal.Should().Be(400m);
        result.DiscountPercent.Should().Be(10m);
        // Recomputed from rates (400 * 11% - 400 * 2% = 44 - 8), not copied from the stored
        // BudgetPlanItem.GrandTotal, which can go stale relative to BudgetPlanTotal/rates.
        result.TaxInclusiveGrandTotal.Should().Be(396m);
        await _apRepo.DidNotReceive().CreateAsync(Arg.Any<AccountPayable>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PreviewAsync_DiscountExceedsDpp_DoesNotThrow()
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
            Quantity = 1m,
            TotalValue = 100m,
            GrandTotal = 100m,
        };

        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var request = new PreviewAccountPayableRequest(22, [100], DiscountAmount: 999m);

        var result = await sut.PreviewAsync(1L, request, ct);

        result.TotalVariance.Should().BeLessThan(0);
    }

    [Fact]
    public async Task PreviewAsync_AppliesWarehouseScopeToRepository()
    {
        var request = new PreviewAccountPayableRequest(22, [1], 0m);

        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>?>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _apRepo.GetAvailabilityDiagnosticsAsync(Arg.Any<long>(), Arg.Any<List<long>>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var sut = CreateSut();
        var ct = TestContext.Current.CancellationToken;
        var act = () => sut.PreviewAsync(1L, request, ct);

        // An empty availability list makes ValidateAllItemsAvailable throw; the
        // assertion under test is the Received(1) call below, not the return value.
        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();

        await _apRepo.Received(1).GetAvailableItemsAsync(
            22, Arg.Any<List<long>>(), Arg.Any<long?>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAndGenerateAsync_SapRejects_SoftDeletesTheDraftItJustCreated()
    {
        var bpi = new BudgetPlanItem
        {
            Id = 100,
            BudgetPlanId = 5L,
            BudgetPlan = new BudgetPlan { Id = 5L, Code = "BP-2026-001", Warehouse = new WarehouseShadow { Code = "WH-01" } },
            ItemShadowId = 11,
            Item = new ItemShadow { Id = 11, ItemCode = "ITEM-A", ItemName = "Item Alpha", AcctCode = "ACC-01", AcctName = "Acct Alpha" },
            VendorShadowId = 22,
            Vendor = new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" },
            UomMasterId = 33,
            Uom = new UomMaster { Id = 33, Code = "PCS", Name = "Pieces" },
            CostValue = 100m,
            Quantity = 2m,
            TotalValue = 200m,
            GrandTotal = 200m,
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Test User", Email = "test@example.com" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>()).Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(1L);
        _apRepo.CreateAsync(Arg.Do<AccountPayable>(ap => ap.Id = 77L), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _apRepo.GetByIdWithItemsAsync(77L, Arg.Any<CancellationToken>()).Returns(new AccountPayable
        {
            Id = 77L,
            Code = "AP-001",
            VendorShadowId = 22L,
            Status = WAMS.Domain.Enums.AccountPayableStatus.Draft,
            Items = [new AccountPayableItem
            {
                VendorCode = "V-01", IsRfba = false, BudgetPlanItemId = 100L,
                BudgetPlanItem = new BudgetPlanItem
                {
                    BudgetPlan = new BudgetPlan { Warehouse = new WarehouseShadow { Code = "WH-01" } }
                }
            }]
        });
        _apRepo.TryClaimForGenerationAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _sapClient.CreateApInvoiceAsync(Arg.Any<SapCreateApInvoiceRequest>(), Arg.Any<CancellationToken>())
            .Returns<SapCreateApInvoiceResult?>(_ => throw new WAMS.Domain.Exceptions.ValidationException("SAP rejected AP-001"));
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        var request = new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]);
        var sut = CreateSut();
        var act = () => sut.CreateAndGenerateAsync(1L, request, CancellationToken.None);

        await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        await _apRepo.Received(1).SoftDeleteAsync(77L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ItemsMissingPo_ThrowsValidationException()
    {
        var item = new AccountPayableItem
        {
            Id = 1, BudgetPlanItemId = 100L,
            VendorCode = "V-01", ItemCode = "ITEM-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 2m, BudgetPlanTotal = 200m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, (int, int)>());

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(1L, userId: 1L, ct: TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Contain("100");
        ex.Which.Message.Should().Contain("no generated purchase order");
    }

    [Fact]
    public async Task GenerateAsync_MixedPoPresence_ThrowsWithOnlyMissingIds()
    {
        var item1 = new AccountPayableItem
        {
            Id = 1, BudgetPlanItemId = 100L,
            VendorCode = "V-01", ItemCode = "ITEM-A", CoaCode = "ACC-01", UomCode = "PCS",
            UnitCost = 100m, UnitCount = 2m, BudgetPlanTotal = 200m,
            IsRfba = false,
        };
        var item2 = new AccountPayableItem
        {
            Id = 2, BudgetPlanItemId = 200L,
            VendorCode = "V-01", ItemCode = "ITEM-B", CoaCode = "ACC-02", UomCode = "PCS",
            UnitCost = 50m, UnitCount = 1m, BudgetPlanTotal = 50m,
            IsRfba = false,
        };
        var ap = BuildGeneratableAp(item1, item2);

        _apRepo.GetByIdWithItemsAsync(1L, Arg.Any<CancellationToken>()).Returns(ap);
        _apRepo.TryClaimForGenerationAsync(1L, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, (int, int)> { [100L] = (501, 0) });

        var sut = CreateSut();
        var act = () => sut.GenerateAsync(1L, userId: 1L, ct: TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<WAMS.Domain.Exceptions.ValidationException>();
        ex.Which.Message.Should().Contain("200");
        ex.Which.Message.Should().NotContain("100");
    }

    [Fact]
    public async Task CreateAsync_ItemsWithoutPo_ReturnsWarnings()
    {
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001", Warehouse = new WarehouseShadow { Id = 1L, Code = "WH-01", Name = "WH Alpha" } };
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
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, (int, int)>());

        var sut = CreateSut();
        var result = await sut.CreateAsync(1L, new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]),
            TestContext.Current.CancellationToken);

        result.Warnings.Should().NotBeNull();
        result.Warnings.Should().ContainSingle();
        result.Warnings![0].Should().Contain("100");
    }

    [Fact]
    public async Task CreateAsync_AllItemsHavePo_WarningsNull()
    {
        var bp = new BudgetPlan { Id = 5L, Code = "BP-2026-001", Warehouse = new WarehouseShadow { Id = 1L, Code = "WH-01", Name = "WH Alpha" } };
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
        };

        _vendorRepo.GetByIdAsync(22, Arg.Any<CancellationToken>())
            .Returns(new VendorShadow { Id = 22, CardCode = "V-01", CardName = "Vendor Alpha" });
        _userRepo.GetByIdAsync(1L, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 1L, Fullname = "Creator" });
        _apRepo.GetAvailableItemsAsync(22, Arg.Any<List<long>>(), Arg.Any<long?>(), Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns([bpi]);
        _codeCounterRepo.NextValueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(1L);
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

        _poRepo.GetGeneratedPoLineRefsAsync(Arg.Any<List<long>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<long, (int, int)> { [100L] = (501, 0) });

        var sut = CreateSut();
        var result = await sut.CreateAsync(1L, new CreateAccountPayableRequest(22, null, DateTime.UtcNow, [100]),
            TestContext.Current.CancellationToken);

        result.Warnings.Should().BeNull();
    }
}
