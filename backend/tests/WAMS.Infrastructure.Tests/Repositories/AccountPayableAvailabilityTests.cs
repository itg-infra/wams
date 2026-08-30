using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.AccountPayables;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.RecapWorkOrders;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.AccountPayables;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

// Mirror of PurchaseOrderAvailabilityTests. See that file's header comment for why
// these parity tests exist. AP availability additionally requires an Approved
// RecapWorkOrder on the budget plan -- without it every query returns empty and
// these tests would pass without proving anything.
public class AccountPayableAvailabilityTests
{
    private static (DbContextOptions<AppDbContext> options, SqliteConnection connection) NewDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using (var db = new AppDbContext(options, Substitute.For<ITenantContext>()))
        {
            db.Database.EnsureCreated();
        }
        return (options, connection);
    }

    private static AppDbContext Open(DbContextOptions<AppDbContext> o)
        => new(o, Substitute.For<ITenantContext>());

    private sealed record Seed(long VendorShadowId, long BudgetPlanId, long BudgetPlanItemId, long AccountPayableId, long WarehouseShadowId);

    private static async Task<Seed> SeedAsync(
        DbContextOptions<AppDbContext> o,
        AccountPayableStatus? apStatus,
        bool apSoftDeleted = false)
    {
        await using var db = Open(o);

        var company = new Company { Name = "C", Code = "C001", IsActive = true };
        var activityType = new ActivityType { Code = "AT1", Name = "Activity", IsActive = true };
        var uom = new UomMaster { Code = "PCS", Name = "Pieces", IsActive = true };
        db.Companies.Add(company);
        db.ActivityTypes.Add(activityType);
        db.UomMasters.Add(uom);
        await db.SaveChangesAsync();

        var user = new User { Email = "u@t.c", Fullname = "U", CompanyId = company.Id, IsActive = true };
        var warehouse = new WarehouseShadow
        {
            Code = "WH1", Name = "WH1", CompanyId = company.Id,
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow, IsActive = true,
        };
        var vendor = new VendorShadow
        {
            CompanyId = company.Id, CardCode = "V1", CardName = "Vendor One",
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow, IsActive = true,
        };
        var item = new ItemShadow
        {
            CompanyId = company.Id, ItemCode = "I1", ItemName = "Item One",
            AcctCode = "A1", AcctName = "Acct One",
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        db.WarehouseShadows.Add(warehouse);
        db.VendorShadows.Add(vendor);
        db.ItemShadows.Add(item);
        await db.SaveChangesAsync();

        var template = new BudgetTemplate
        {
            Code = "BT1", CompanyId = company.Id,
            Status = BudgetTemplateStatus.Submitted, CreatedByUserId = user.Id,
        };
        db.BudgetTemplates.Add(template);
        await db.SaveChangesAsync();

        var plan = new BudgetPlan
        {
            Code = "BP1", CompanyId = company.Id, BudgetTemplateId = template.Id,
            WarehouseShadowId = warehouse.Id, DocDate = DateTime.UtcNow,
            Status = BudgetPlanStatus.Approved, CreatedByUserId = user.Id,
        };
        db.BudgetPlans.Add(plan);
        await db.SaveChangesAsync();

        var planItem = new BudgetPlanItem
        {
            BudgetPlanId = plan.Id, ItemShadowId = item.Id, VendorShadowId = vendor.Id,
            UomMasterId = uom.Id, ActivityTypeId = activityType.Id,
            CostValue = 100m, Quantity = 2m, TotalValue = 200m, GrandTotal = 200m, SortOrder = 1,
        };
        db.BudgetPlanItems.Add(planItem);
        await db.SaveChangesAsync();

        // AP availability gate: the plan must have an Approved RecapWorkOrder.
        // RecapWorkOrder (src/WAMS.Domain/Entities/RecapWorkOrders/RecapWorkOrder.cs) only
        // declares BudgetPlanId, CompanyId and Status as required non-nullable scalars
        // beyond the BaseEntity Id/CreatedAt -- there is no Code or CreatedByUserId field.
        db.RecapWorkOrders.Add(new RecapWorkOrder
        {
            CompanyId = company.Id,
            BudgetPlanId = plan.Id,
            Status = RecapWorkOrderStatus.Approved,
        });
        await db.SaveChangesAsync();

        long apId = 0;
        if (apStatus is not null)
        {
            var ap = new AccountPayable
            {
                Code = "AP-1", CompanyId = company.Id, VendorShadowId = vendor.Id,
                DocDate = DateTime.UtcNow, Status = apStatus, CreatedByUserId = user.Id,
                DeletedAt = apSoftDeleted ? DateTime.UtcNow : null,
            };
            db.AccountPayables.Add(ap);
            await db.SaveChangesAsync();

            // Mirror the snapshot fields AccountPayableService.BuildItems sets.
            // AccountPayableItem (src/WAMS.Domain/Entities/AccountPayables/AccountPayableItem.cs)
            // has no ItemShadowId/UomMasterId/CostValue/Quantity/TotalValue fields like
            // PurchaseOrderItem does -- it uses UnitCost/UnitCount/BudgetPlanTotal instead
            // and has no ItemShadowId or UomMasterId FK columns at all.
            db.AccountPayableItems.Add(new AccountPayableItem
            {
                AccountPayableId = ap.Id, BudgetPlanItemId = planItem.Id,
                ItemCode = "I1", ItemName = "Item One",
                CoaCode = "A1", CoaName = "Acct One",
                VendorShadowId = vendor.Id, VendorCode = "V1", VendorName = "Vendor One",
                UomCode = "PCS", UomName = "Pieces",
                UnitCost = 100m, UnitCount = 2m, BudgetPlanTotal = 200m, GrandTotal = 200m, SortOrder = 1,
            });
            await db.SaveChangesAsync();
            apId = ap.Id;
        }

        return new Seed(vendor.Id, plan.Id, planItem.Id, apId, warehouse.Id);
    }

    private static async Task<(List<long> Picker, List<long> CreatePath)> BothPathsAsync(
        AccountPayableRepository repo, Seed seed, long? excludeDocumentId, CancellationToken ct)
    {
        var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
            seed.VendorShadowId, [seed.BudgetPlanId], false, excludeDocumentId, null, ct);
        var createPath = await repo.GetAvailableItemsAsync(
            seed.VendorShadowId, [], excludeDocumentId, null, ct);
        return (picker.Select(x => x.BudgetPlanItemId).ToList(), createPath.Select(x => x.Id).ToList());
    }

    [Fact]
    public async Task Fixture_IsNotVacuous_ItemVisibleWhenOnNoAp()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var (picker, createPath) = await BothPathsAsync(
                repo, seed, null, TestContext.Current.CancellationToken);

            picker.Should().Equal(seed.BudgetPlanItemId);
            createPath.Should().Equal(seed.BudgetPlanItemId);
        }
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Generated")]
    public async Task PickerAndCreatePath_AgreeForEveryApStatus(string apStatusValue)
    {
        // AccountPayableStatus is an Ardalis SmartEnum, not a native enum, so its
        // members are not compile-time constants usable directly in [InlineData].
        var apStatus = AccountPayableStatus.FromValue(apStatusValue);
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, apStatus);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var (picker, createPath) = await BothPathsAsync(
                repo, seed, null, TestContext.Current.CancellationToken);

            picker.Should().BeEquivalentTo(createPath);
            picker.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Picker_ShowsItemBackWhenExcludingTheDraftApThatHoldsIt()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, AccountPayableStatus.Draft);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], false, seed.AccountPayableId, null,
                TestContext.Current.CancellationToken);

            picker.Select(x => x.BudgetPlanItemId).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    [Fact]
    public async Task Picker_IgnoresSoftDeletedAccountPayables()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, AccountPayableStatus.Draft, apSoftDeleted: true);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], false, null, null,
                TestContext.Current.CancellationToken);

            picker.Select(x => x.BudgetPlanItemId).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    // Proves GetAvailabilityDiagnosticsAsync's TakenByCode projection (a correlated
    // OrderByDescending().Select().FirstOrDefault() subquery reusing the same
    // TakenByAnotherAccountPayable filter as the boolean availability predicate) actually
    // translates to SQL via a real engine, not just the EF InMemory provider.
    [Fact]
    public async Task Diagnostics_ItemHeldByDraftAp_PopulatesTakenByCodeWithThatApsCode()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, AccountPayableStatus.Draft);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var diagnostics = await repo.GetAvailabilityDiagnosticsAsync(
                seed.VendorShadowId, [seed.BudgetPlanItemId], null, TestContext.Current.CancellationToken);

            diagnostics.Should().HaveCount(1);
            diagnostics[0].AlreadyGenerated.Should().BeFalse();
            diagnostics[0].TakenByCode.Should().Be("AP-1");
        }
    }

    [Fact]
    public async Task Diagnostics_ItemOnNoAp_TakenByCodeIsNull()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var diagnostics = await repo.GetAvailabilityDiagnosticsAsync(
                seed.VendorShadowId, [seed.BudgetPlanItemId], null, TestContext.Current.CancellationToken);

            diagnostics.Should().HaveCount(1);
            diagnostics[0].TakenByCode.Should().BeNull();
        }
    }

    // includeGenerated=true returns taken items instead of filtering them out, so it is the
    // only mode where the caller must tell "free" from "held" per row. isGenerated cannot do
    // that job alone: it is false for an item held by a Draft AP, making it indistinguishable
    // from a genuinely free item. takenByCode is what closes that gap, so these three cases
    // pin all three states the FE has to render differently.
    [Fact]
    public async Task Picker_IncludeGenerated_FreeItem_TakenByCodeIsNull()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], true, null, null,
                TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            picker[0].IsGenerated.Should().BeFalse();
            picker[0].TakenByCode.Should().BeNull();
        }
    }

    [Fact]
    public async Task Picker_IncludeGenerated_ItemHeldByDraftAp_HasTakenByCodeButIsGeneratedFalse()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, AccountPayableStatus.Draft);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], true, null, null,
                TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            // The whole point: isGenerated stays false (it IS only about Generated APs) while
            // takenByCode reveals the Draft holding it. Before takenByCode existed this row was
            // byte-identical to the free-item row above.
            picker[0].IsGenerated.Should().BeFalse();
            picker[0].TakenByCode.Should().Be("AP-1");
        }
    }

    [Fact]
    public async Task Picker_IncludeGenerated_ItemOnGeneratedAp_HasBothTakenByCodeAndIsGenerated()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, AccountPayableStatus.Generated);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], true, null, null,
                TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            picker[0].IsGenerated.Should().BeTrue();
            picker[0].TakenByCode.Should().Be("AP-1");
        }
    }

    // The !includeGenerated branch projects the constant null rather than running the holder
    // subquery, on the grounds that its filter already removed every taken row. This asserts
    // that shortcut stays truthful.
    [Fact]
    public async Task Picker_ExcludeGenerated_EveryReturnedRowHasNullTakenByCode()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], false, null, null,
                TestContext.Current.CancellationToken);

            picker.Should().NotBeEmpty();
            picker.Should().OnlyContain(x => x.TakenByCode == null);
        }
    }

    // Editing a Draft AP must not report that AP as holding its own items hostage.
    [Fact]
    public async Task Picker_IncludeGenerated_ExcludingHoldingDraft_TakenByCodeIsNull()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, AccountPayableStatus.Draft);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], true, seed.AccountPayableId, null,
                TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            picker[0].TakenByCode.Should().BeNull();
        }
    }

    // The picker (used to populate the "available items" list a maker can pull from) must
    // never offer an item that lives in a warehouse the caller's warehouseIds list doesn't
    // include, even though vendor/recap-approval checks all pass.
    [Fact]
    public async Task Picker_ExcludesItemOutsideGivenWarehouseIds()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());
            var otherWarehouseId = seed.WarehouseShadowId + 1000;

            var restricted = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], false, null, [otherWarehouseId],
                TestContext.Current.CancellationToken);
            var inScope = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], false, null, [seed.WarehouseShadowId],
                TestContext.Current.CancellationToken);

            restricted.Should().BeEmpty();
            inScope.Select(x => x.BudgetPlanItemId).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    // Same rule on the create/update validation path (GetAvailableItemsAsync) -- this is what
    // actually blocks a direct API call carrying an out-of-scope budgetPlanItemId, independent
    // of whether the picker ever offered it.
    [Fact]
    public async Task CreatePath_ExcludesItemOutsideGivenWarehouseIds()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());
            var otherWarehouseId = seed.WarehouseShadowId + 1000;

            var restricted = await repo.GetAvailableItemsAsync(
                seed.VendorShadowId, [], null, [otherWarehouseId], TestContext.Current.CancellationToken);
            var inScope = await repo.GetAvailableItemsAsync(
                seed.VendorShadowId, [], null, [seed.WarehouseShadowId], TestContext.Current.CancellationToken);

            restricted.Should().BeEmpty();
            inScope.Select(x => x.Id).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    // A null warehouseIds list means "no filter" (global access) -- it must return the same
    // rows as omitting the filter entirely, never an empty result.
    [Fact]
    public async Task NullWarehouseIds_MeansUnrestricted_NotMatchNothing()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());

            var picker = await repo.GetAvailableItemsByBudgetPlansAsync(
                seed.VendorShadowId, [seed.BudgetPlanId], false, null, null,
                TestContext.Current.CancellationToken);
            var createPath = await repo.GetAvailableItemsAsync(
                seed.VendorShadowId, [], null, null, TestContext.Current.CancellationToken);

            picker.Select(x => x.BudgetPlanItemId).Should().Equal(seed.BudgetPlanItemId);
            createPath.Select(x => x.Id).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    // Diagnostics must surface the warehouse mismatch as its own flag (not silently reported as
    // some other reason) so the service layer can emit the specific, actionable error message.
    [Fact]
    public async Task Diagnostics_ItemOutsideGivenWarehouseIds_WarehouseInScopeIsFalse()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new AccountPayableRepository(db, Substitute.For<ITenantContext>());
            var otherWarehouseId = seed.WarehouseShadowId + 1000;

            var diagnostics = await repo.GetAvailabilityDiagnosticsAsync(
                seed.VendorShadowId, [seed.BudgetPlanItemId], [otherWarehouseId], TestContext.Current.CancellationToken);

            diagnostics.Should().HaveCount(1);
            diagnostics[0].Found.Should().BeTrue();
            diagnostics[0].VendorMatches.Should().BeTrue();
            diagnostics[0].WarehouseInScope.Should().BeFalse();
        }
    }
}
