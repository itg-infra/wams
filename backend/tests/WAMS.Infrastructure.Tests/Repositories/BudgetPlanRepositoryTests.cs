using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.BudgetPlans;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

public class BudgetPlanRepositoryTests
{
    // SQLite in-memory DB is used (not the InMemory provider) so this test exercises
    // EF's real global query filter translation - the InMemory provider does not
    // reliably reproduce the filter-leak bug this test guards against.
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

    private static async Task<long> SeedBudgetPlanWithWorkOrdersAsync(
        DbContextOptions<AppDbContext> o, bool includeSoftDeletedWorkOrder)
    {
        await using var db = Open(o);

        var company = new Company { Name = "C", Code = "C001", IsActive = true };
        var activityType = new ActivityType { Code = "AT1", Name = "Activity", IsActive = true };
        db.Companies.Add(company);
        db.ActivityTypes.Add(activityType);
        await db.SaveChangesAsync();

        var user = new User { Email = "u@t.c", Fullname = "U", CompanyId = company.Id, IsActive = true };
        var warehouse = new WarehouseShadow
        {
            Code = "WH1", Name = "WH1", CompanyId = company.Id,
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow, IsActive = true,
        };
        db.Users.Add(user);
        db.WarehouseShadows.Add(warehouse);
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

        var itemActive = new ItemShadow
        {
            CompanyId = company.Id, ItemCode = "I1", ItemName = "I1", AcctCode = "A1", AcctName = "A1",
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };
        var itemDeleted = new ItemShadow
        {
            CompanyId = company.Id, ItemCode = "I2", ItemName = "I2", AcctCode = "A2", AcctName = "A2",
            FirstSeenAt = DateTime.UtcNow, SyncedAt = DateTime.UtcNow,
        };
        db.ItemShadows.AddRange(itemActive, itemDeleted);
        await db.SaveChangesAsync();

        var activeWorkOrder = new WorkOrder
        {
            Code = "WO-ACTIVE", CompanyId = company.Id, BudgetPlanId = plan.Id,
            ItemShadowId = itemActive.Id, ActivityTypeCode = "AT1", WarehouseShadowId = warehouse.Id,
            TemplateCode = "BT1", PicUserId = user.Id, CreatedByUserId = user.Id,
        };
        db.WorkOrders.Add(activeWorkOrder);

        if (includeSoftDeletedWorkOrder)
        {
            var deletedWorkOrder = new WorkOrder
            {
                Code = "WO-DELETED", CompanyId = company.Id, BudgetPlanId = plan.Id,
                ItemShadowId = itemDeleted.Id, ActivityTypeCode = "AT1", WarehouseShadowId = warehouse.Id,
                TemplateCode = "BT1", PicUserId = user.Id, CreatedByUserId = user.Id,
                DeletedAt = DateTime.UtcNow,
            };
            db.WorkOrders.Add(deletedWorkOrder);
        }

        await db.SaveChangesAsync();
        return plan.Id;
    }

    [Fact]
    public async Task GetByIdWithItemsAndWorkOrdersAsync_IncludesSoftDeletedWorkOrders()
    {
        var (opts, connection) = NewDb();
        using (connection)
        {
            var planId = await SeedBudgetPlanWithWorkOrdersAsync(opts, includeSoftDeletedWorkOrder: true);

            await using var db = Open(opts);
            var repo = new BudgetPlanRepository(db, Substitute.For<ITenantContext>());

            var plan = await repo.GetByIdWithItemsAndWorkOrdersAsync(planId, TestContext.Current.CancellationToken);

            plan.Should().NotBeNull();
            plan!.WorkOrders.Select(w => w.Code).Should().BeEquivalentTo(["WO-ACTIVE", "WO-DELETED"]);
        }
    }

    [Fact]
    public async Task GetByIdWithItemsAndWorkOrdersAsync_WithOnlyActiveWorkOrders_ReturnsJustThat()
    {
        var (opts, connection) = NewDb();
        using (connection)
        {
            var planId = await SeedBudgetPlanWithWorkOrdersAsync(opts, includeSoftDeletedWorkOrder: false);

            await using var db = Open(opts);
            var repo = new BudgetPlanRepository(db, Substitute.For<ITenantContext>());

            var plan = await repo.GetByIdWithItemsAndWorkOrdersAsync(planId, TestContext.Current.CancellationToken);

            plan.Should().NotBeNull();
            plan!.WorkOrders.Select(w => w.Code).Should().BeEquivalentTo(["WO-ACTIVE"]);
        }
    }
}
