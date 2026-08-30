using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.Common;
using WAMS.Application.DTOs.PurchaseOrders;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.PurchaseOrders;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.PurchaseOrders;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

// These tests exist because the picker endpoint (GetAvailableItemsForPickerAsync)
// and the create/update validation path (GetAvailableItemsAsync) once used two
// hand-written copies of the same "is this item already taken" rule and drifted
// apart: the picker offered items that CreateAsync then rejected with a 400.
// The parity tests below fail if the two paths ever disagree again.
public class PurchaseOrderAvailabilityTests
{
    // SQLite in-memory (not the EF InMemory provider) so the correlated NOT EXISTS
    // subquery is actually translated to SQL and executed by a real engine.
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

    private sealed record Seed(long VendorShadowId, long BudgetPlanId, long BudgetPlanItemId, long PurchaseOrderId, long WarehouseShadowId);

    private sealed record CrossWarehouseSeed(
        long VendorId,
        long SpaWarehouseId,
        long KkWarehouseId,
        long SpaBudgetPlanId,
        long KkBudgetPlanId,
        long SpaItemId,
        long KkItemId,
        long OtherVendorItemId);

    // Seeds one Approved budget plan holding one item for one vendor. When
    // poStatus is non-null, that item is also attached to a PO in that status.
    private static async Task<Seed> SeedAsync(
        DbContextOptions<AppDbContext> o,
        PurchaseOrderStatus? poStatus,
        bool poSoftDeleted = false)
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

        long poId = 0;
        if (poStatus is not null)
        {
            var po = new PurchaseOrder
            {
                Code = "PO-1", CompanyId = company.Id, VendorShadowId = vendor.Id,
                DocDate = DateTime.UtcNow, Status = poStatus, CreatedByUserId = user.Id,
                DeletedAt = poSoftDeleted ? DateTime.UtcNow : null,
            };
            db.PurchaseOrders.Add(po);
            await db.SaveChangesAsync();

            db.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = po.Id, BudgetPlanItemId = planItem.Id,
                ItemShadowId = item.Id, ItemCode = "I1", ItemName = "Item One",
                CoaCode = "A1", CoaName = "Acct One",
                VendorShadowId = vendor.Id, VendorCode = "V1", VendorName = "Vendor One",
                UomMasterId = uom.Id, UomCode = "PCS", UomName = "Pieces",
                CostValue = 100m, Quantity = 2m, TotalValue = 200m, GrandTotal = 200m, SortOrder = 1,
            });
            await db.SaveChangesAsync();
            poId = po.Id;
        }

        return new Seed(vendor.Id, plan.Id, planItem.Id, poId, warehouse.Id);
    }

    private static async Task<CrossWarehouseSeed> SeedCrossWarehouseAsync(
        DbContextOptions<AppDbContext> o)
    {
        await using var db = Open(o);

        var now = new DateTime(2026, 8, 26, 0, 0, 0, DateTimeKind.Utc);
        var company = new Company { Name = "C", Code = "C001", IsActive = true };
        var activityType = new ActivityType { Code = "AT1", Name = "Activity", IsActive = true };
        var uom = new UomMaster { Code = "PCS", Name = "Pieces", IsActive = true };
        db.AddRange(company, activityType, uom);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email = "u@t.c", Fullname = "U", CompanyId = company.Id, IsActive = true,
        };
        var spaWarehouse = new WarehouseShadow
        {
            Code = "WHSBY010", Name = "SBY - SPA", CompanyId = company.Id,
            FirstSeenAt = now, SyncedAt = now, IsActive = true,
        };
        var kkWarehouse = new WarehouseShadow
        {
            Code = "WHSBY017", Name = "SBY - KK", CompanyId = company.Id,
            FirstSeenAt = now, SyncedAt = now, IsActive = true,
        };
        var vendor = new VendorShadow
        {
            CompanyId = company.Id, CardCode = "V1", CardName = "Vendor One",
            FirstSeenAt = now, SyncedAt = now, IsActive = true,
        };
        var otherVendor = new VendorShadow
        {
            CompanyId = company.Id, CardCode = "V2", CardName = "Vendor Two",
            FirstSeenAt = now, SyncedAt = now, IsActive = true,
        };
        var spaItem = new ItemShadow
        {
            CompanyId = company.Id, ItemCode = "I-SPA", ItemName = "SPA Item",
            AcctCode = "A1", AcctName = "Account One",
            FirstSeenAt = now, SyncedAt = now,
        };
        var kkItem = new ItemShadow
        {
            CompanyId = company.Id, ItemCode = "I-KK", ItemName = "KK Item",
            AcctCode = "A2", AcctName = "Account Two",
            FirstSeenAt = now, SyncedAt = now,
        };
        var otherVendorItem = new ItemShadow
        {
            CompanyId = company.Id, ItemCode = "I-OTHER", ItemName = "Other Vendor Item",
            AcctCode = "A3", AcctName = "Account Three",
            FirstSeenAt = now, SyncedAt = now,
        };
        db.AddRange(user, spaWarehouse, kkWarehouse, vendor, otherVendor, spaItem, kkItem, otherVendorItem);
        await db.SaveChangesAsync();

        var template = new BudgetTemplate
        {
            Code = "BT1", CompanyId = company.Id,
            Status = BudgetTemplateStatus.Submitted, CreatedByUserId = user.Id,
        };
        db.BudgetTemplates.Add(template);
        await db.SaveChangesAsync();

        var spaPlan = new BudgetPlan
        {
            Code = "BP-SPA", Remark = "SPA seed", CompanyId = company.Id,
            BudgetTemplateId = template.Id, WarehouseShadowId = spaWarehouse.Id,
            DocDate = now, Status = BudgetPlanStatus.Approved, CreatedByUserId = user.Id,
        };
        var kkPlan = new BudgetPlan
        {
            Code = "BP-KK", Remark = "KK suggestion", CompanyId = company.Id,
            BudgetTemplateId = template.Id, WarehouseShadowId = kkWarehouse.Id,
            DocDate = now.AddDays(1), Status = BudgetPlanStatus.Approved, CreatedByUserId = user.Id,
        };
        db.AddRange(spaPlan, kkPlan);
        await db.SaveChangesAsync();

        var spaPlanItem = new BudgetPlanItem
        {
            BudgetPlanId = spaPlan.Id, ItemShadowId = spaItem.Id, VendorShadowId = vendor.Id,
            UomMasterId = uom.Id, ActivityTypeId = activityType.Id,
            CostValue = 100m, Quantity = 1m, TotalValue = 100m, GrandTotal = 100m,
            BillOfLading = "BOL-SPA", SortOrder = 1,
        };
        var kkPlanItem = new BudgetPlanItem
        {
            BudgetPlanId = kkPlan.Id, ItemShadowId = kkItem.Id, VendorShadowId = vendor.Id,
            UomMasterId = uom.Id, ActivityTypeId = activityType.Id,
            CostValue = 200m, Quantity = 2m, TotalValue = 400m, GrandTotal = 400m,
            BillOfLading = "BOL-KK", SortOrder = 1,
        };
        var otherVendorPlanItem = new BudgetPlanItem
        {
            BudgetPlanId = spaPlan.Id, ItemShadowId = otherVendorItem.Id, VendorShadowId = otherVendor.Id,
            UomMasterId = uom.Id, ActivityTypeId = activityType.Id,
            CostValue = 300m, Quantity = 3m, TotalValue = 900m, GrandTotal = 900m,
            BillOfLading = "BOL-OTHER", SortOrder = 2,
        };
        db.AddRange(spaPlanItem, kkPlanItem, otherVendorPlanItem);
        await db.SaveChangesAsync();

        return new CrossWarehouseSeed(
            vendor.Id,
            spaWarehouse.Id,
            kkWarehouse.Id,
            spaPlan.Id,
            kkPlan.Id,
            spaPlanItem.Id,
            kkPlanItem.Id,
            otherVendorPlanItem.Id);
    }

    [Fact]
    public async Task Picker_SeedVendor_ReturnsAccessibleItemsAcrossWarehouses()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedCrossWarehouseAsync(opts);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var (items, total) = await repo.GetAvailableItemsForPickerAsync(
                [seed.VendorId], seed.SpaBudgetPlanId,
                new DataTableQuery { Page = 1, Limit = 20 },
                false, null, [seed.SpaWarehouseId, seed.KkWarehouseId],
                TestContext.Current.CancellationToken);

            total.Should().Be(2);
            items.Should().SatisfyRespectively(
                spa =>
                {
                    spa.BudgetPlanItemId.Should().Be(seed.SpaItemId);
                    spa.BudgetPlanId.Should().Be(seed.SpaBudgetPlanId);
                    spa.IsSeedBudgetPlan.Should().BeTrue();
                    spa.WarehouseShadowId.Should().Be(seed.SpaWarehouseId);
                    spa.WarehouseCode.Should().Be("WHSBY010");
                    spa.WarehouseName.Should().Be("SBY - SPA");
                    spa.VendorShadowId.Should().Be(seed.VendorId);
                },
                kk =>
                {
                    kk.BudgetPlanItemId.Should().Be(seed.KkItemId);
                    kk.BudgetPlanId.Should().Be(seed.KkBudgetPlanId);
                    kk.IsSeedBudgetPlan.Should().BeFalse();
                    kk.WarehouseShadowId.Should().Be(seed.KkWarehouseId);
                    kk.WarehouseCode.Should().Be("WHSBY017");
                    kk.WarehouseName.Should().Be("SBY - KK");
                    kk.VendorShadowId.Should().Be(seed.VendorId);
                });
            items.Should().NotContain(x => x.BudgetPlanItemId == seed.OtherVendorItemId);
        }
    }

    [Fact]
    public async Task Picker_WarehouseScopeExcludesInaccessibleSuggestion()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedCrossWarehouseAsync(opts);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var (items, total) = await repo.GetAvailableItemsForPickerAsync(
                [seed.VendorId], seed.SpaBudgetPlanId,
                new DataTableQuery(), false, null, [seed.SpaWarehouseId],
                TestContext.Current.CancellationToken);

            total.Should().Be(1);
            items.Should().ContainSingle(x => x.BudgetPlanItemId == seed.SpaItemId);
        }
    }

    // Both paths are asked the same question. Path A is given an empty item-id list,
    // which its `budgetPlanItemIds.Count == 0` branch treats as "no id filter"; the
    // fixture holds exactly one plan, so the two queries cover the same rows.
    private static async Task<(List<long> Picker, List<long> CreatePath)> BothPathsAsync(
        PurchaseOrderRepository repo, Seed seed, long? excludeDocumentId, CancellationToken ct)
    {
        var picker = await PickerAsync(repo, seed, false, excludeDocumentId, null, ct);
        var createPath = await repo.GetAvailableItemsAsync(
            seed.VendorShadowId, [], excludeDocumentId, null, ct);
        return (picker.Select(x => x.BudgetPlanItemId).ToList(), createPath.Select(x => x.Id).ToList());
    }

    private static async Task<List<AvailablePoItemResponse>> PickerAsync(
        PurchaseOrderRepository repo,
        Seed seed,
        bool includeGenerated,
        long? excludeDocumentId,
        List<long>? warehouseIds,
        CancellationToken ct)
    {
        var (items, _) = await repo.GetAvailableItemsForPickerAsync(
            [seed.VendorShadowId],
            seed.BudgetPlanId,
            new DataTableQuery { Limit = 100 },
            includeGenerated,
            excludeDocumentId,
            warehouseIds,
            ct);
        return items;
    }

    [Theory]
    [InlineData(null)]         // item on no PO at all
    [InlineData("Draft")]      // the case that used to diverge
    [InlineData("Generated")]
    public async Task PickerAndCreatePath_AgreeForEveryPoStatus(string? poStatusValue)
    {
        // PurchaseOrderStatus is an Ardalis SmartEnum, not a native enum, so its
        // members are not compile-time constants usable directly in [InlineData].
        var poStatus = poStatusValue is null ? null : PurchaseOrderStatus.FromValue(poStatusValue);
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, poStatus);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var (picker, createPath) = await BothPathsAsync(
                repo, seed, null, TestContext.Current.CancellationToken);

            picker.Should().BeEquivalentTo(createPath);
        }
    }

    [Fact]
    public async Task Picker_HidesItemHeldByAnotherDraftPo()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Draft);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, false, null, null, TestContext.Current.CancellationToken);

            picker.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Picker_ShowsItemBackWhenExcludingTheDraftPoThatHoldsIt()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Draft);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, false, seed.PurchaseOrderId, null,
                TestContext.Current.CancellationToken);

            picker.Select(x => x.BudgetPlanItemId).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    [Fact]
    public async Task Picker_IgnoresSoftDeletedPurchaseOrders()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Draft, poSoftDeleted: true);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, false, null, null, TestContext.Current.CancellationToken);

            picker.Select(x => x.BudgetPlanItemId).Should().Equal(seed.BudgetPlanItemId);
        }
    }

    // Proves GetAvailabilityDiagnosticsAsync's TakenByCode projection (a correlated
    // OrderByDescending().Select().FirstOrDefault() subquery reusing the same
    // TakenByAnotherPurchaseOrder filter as the boolean availability predicate) actually
    // translates to SQL via a real engine, not just the EF InMemory provider.
    [Fact]
    public async Task Diagnostics_ItemHeldByDraftPo_PopulatesTakenByCodeWithThatPosCode()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Draft);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var diagnostics = await repo.GetAvailabilityDiagnosticsAsync(
                seed.VendorShadowId, [seed.BudgetPlanItemId], null, TestContext.Current.CancellationToken);

            diagnostics.Should().HaveCount(1);
            diagnostics[0].AlreadyGenerated.Should().BeFalse();
            diagnostics[0].TakenByCode.Should().Be("PO-1");
        }
    }

    [Fact]
    public async Task Diagnostics_ItemOnNoPo_TakenByCodeIsNull()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var diagnostics = await repo.GetAvailabilityDiagnosticsAsync(
                seed.VendorShadowId, [seed.BudgetPlanItemId], null, TestContext.Current.CancellationToken);

            diagnostics.Should().HaveCount(1);
            diagnostics[0].TakenByCode.Should().BeNull();
        }
    }

    // includeGenerated=true returns taken items instead of filtering them out, so it is the
    // only mode where the caller must tell "free" from "held" per row. isGenerated cannot do
    // that job alone: it is false for an item held by a Draft PO, making it indistinguishable
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
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, true, null, null, TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            picker[0].IsGenerated.Should().BeFalse();
            picker[0].TakenByCode.Should().BeNull();
        }
    }

    [Fact]
    public async Task Picker_IncludeGenerated_ItemHeldByDraftPo_HasTakenByCodeButIsGeneratedFalse()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Draft);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, true, null, null, TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            // The whole point: isGenerated stays false (it IS only about Generated POs) while
            // takenByCode reveals the Draft holding it. Before takenByCode existed this row was
            // byte-identical to the free-item row above.
            picker[0].IsGenerated.Should().BeFalse();
            picker[0].TakenByCode.Should().Be("PO-1");
        }
    }

    [Fact]
    public async Task Picker_IncludeGenerated_ItemOnGeneratedPo_HasBothTakenByCodeAndIsGenerated()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Generated);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, true, null, null, TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            picker[0].IsGenerated.Should().BeTrue();
            picker[0].TakenByCode.Should().Be("PO-1");
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
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, false, null, null, TestContext.Current.CancellationToken);

            picker.Should().NotBeEmpty();
            picker.Should().OnlyContain(x => x.TakenByCode == null);
        }
    }

    // Editing a Draft PO must not report that PO as holding its own items hostage.
    [Fact]
    public async Task Picker_IncludeGenerated_ExcludingHoldingDraft_TakenByCodeIsNull()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, PurchaseOrderStatus.Draft);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, true, seed.PurchaseOrderId, null,
                TestContext.Current.CancellationToken);

            picker.Should().HaveCount(1);
            picker[0].TakenByCode.Should().BeNull();
        }
    }

    // The picker (used to populate the "available items" list a maker can pull from) must
    // never offer an item that lives in a warehouse the caller's warehouseIds list doesn't
    // include, even though vendor/plan-approval checks all pass.
    [Fact]
    public async Task Picker_ExcludesItemOutsideGivenWarehouseIds()
    {
        var (opts, conn) = NewDb();
        using (conn)
        {
            var seed = await SeedAsync(opts, null);
            await using var db = Open(opts);
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());
            var otherWarehouseId = seed.WarehouseShadowId + 1000;

            var restricted = await PickerAsync(
                repo, seed, false, null, [otherWarehouseId],
                TestContext.Current.CancellationToken);
            var inScope = await PickerAsync(
                repo, seed, false, null, [seed.WarehouseShadowId],
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
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());
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
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());

            var picker = await PickerAsync(
                repo, seed, false, null, null, TestContext.Current.CancellationToken);
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
            var repo = new PurchaseOrderRepository(db, Substitute.For<ITenantContext>());
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
