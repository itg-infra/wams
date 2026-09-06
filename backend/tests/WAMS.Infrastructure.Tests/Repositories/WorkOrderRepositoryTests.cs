using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Constants;
using WAMS.Domain.Entities.ActivityTypes;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Items;
using WAMS.Domain.Entities.Uoms;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Vendors;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Entities.WorkOrders;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.WorkOrders;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

public class WorkOrderRepositoryTests
{
    private static (DbContextOptions<AppDbContext> options, SqliteConnection connection) NewDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        using (var db = new AppDbContext(options, Substitute.For<ITenantContext>()))
            db.Database.EnsureCreated();
        return (options, connection);
    }

    private static AppDbContext Open(DbContextOptions<AppDbContext> options) =>
        new(options, Substitute.For<ITenantContext>());

    private static async Task<long> SeedWorkOrderAsync(
        DbContextOptions<AppDbContext> options,
        string activityTypeCode)
    {
        await using var db = Open(options);

        var company = new Company { Name = "Company", Code = "C001", IsActive = true };
        var activityType = new ActivityType { Code = activityTypeCode, Name = activityTypeCode, IsActive = true };
        db.Companies.Add(company);
        db.ActivityTypes.Add(activityType);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email = "user@example.com",
            Fullname = "Test User",
            CompanyId = company.Id,
            IsActive = true,
        };
        var warehouse = new WarehouseShadow
        {
            Code = "WH1",
            Name = "Warehouse 1",
            CompanyId = company.Id,
            FirstSeenAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            IsActive = true,
        };
        var template = new BudgetTemplate
        {
            Code = "BT1",
            CompanyId = company.Id,
            Status = BudgetTemplateStatus.Submitted,
            CreatedByUserId = user.Id,
        };
        db.Users.Add(user);
        db.WarehouseShadows.Add(warehouse);
        await db.SaveChangesAsync();
        template.CreatedByUserId = user.Id;
        db.BudgetTemplates.Add(template);
        await db.SaveChangesAsync();

        var plan = new BudgetPlan
        {
            Code = "BP1",
            CompanyId = company.Id,
            BudgetTemplateId = template.Id,
            WarehouseShadowId = warehouse.Id,
            DocDate = DateTime.UtcNow,
            Status = BudgetPlanStatus.Approved,
            CreatedByUserId = user.Id,
        };
        var item = new ItemShadow
        {
            CompanyId = company.Id,
            ItemCode = "I1",
            ItemName = "Item 1",
            AcctCode = "A1",
            AcctName = "Account 1",
            FirstSeenAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            IsActive = true,
        };
        var vendor = new VendorShadow
        {
            CompanyId = company.Id,
            CardCode = "V1",
            CardName = "Vendor 1",
            FirstSeenAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow,
            IsActive = true,
        };
        var uom = new UomMaster { Code = "KG", Name = "Kilogram", IsActive = true };
        db.BudgetPlans.Add(plan);
        db.ItemShadows.Add(item);
        db.VendorShadows.Add(vendor);
        db.UomMasters.Add(uom);
        await db.SaveChangesAsync();

        var planItem = new BudgetPlanItem
        {
            BudgetPlanId = plan.Id,
            ItemShadowId = item.Id,
            ActivityTypeId = activityType.Id,
            VendorShadowId = vendor.Id,
            UomMasterId = uom.Id,
            CostValue = 1,
            Quantity = 1,
            TotalValue = 1,
            SortOrder = 1,
        };
        db.BudgetPlanItems.Add(planItem);
        await db.SaveChangesAsync();

        var workOrder = new WorkOrder
        {
            Code = "WO1",
            CompanyId = company.Id,
            BudgetPlanId = plan.Id,
            BudgetPlanItemId = planItem.Id,
            ItemShadowId = item.Id,
            ActivityTypeCode = activityTypeCode,
            WarehouseShadowId = warehouse.Id,
            TemplateCode = template.Code,
            PicUserId = user.Id,
            CreatedByUserId = user.Id,
            Status = WorkOrderStatus.Draft,
        };
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        db.WorkOrderStorageDetails.Add(new WorkOrderStorageDetail
        {
            WorkOrderId = workOrder.Id,
            VolumeWeight = 125,
            WorkerOnDuty = 3,
        });
        await db.SaveChangesAsync();
        return workOrder.Id;
    }

    [Theory]
    [InlineData(ActivityTypeCodes.Gudang, "storage")]
    [InlineData(ActivityTypeCodes.Opname, "opname")]
    [InlineData(ActivityTypeCodes.Others, "others")]
    public async Task GetByIdProjectionAsync_RoutesStorageDetailToActivityProperty(
        string activityTypeCode,
        string expectedProperty)
    {
        var (options, connection) = NewDb();
        using (connection)
        {
            var workOrderId = await SeedWorkOrderAsync(options, activityTypeCode);
            await using var db = Open(options);
            var sut = new WorkOrderRepository(db, Substitute.For<ITenantContext>());

            var result = await sut.GetByIdProjectionAsync(workOrderId, TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            var selected = expectedProperty switch
            {
                "storage" => result!.Storage,
                "opname" => result.Opname,
                "others" => result.Others,
                _ => null,
            };
            selected.Should().NotBeNull();
            selected!.VolumeWeight.Should().Be(125m);
            if (expectedProperty != "storage") result.Storage.Should().BeNull();
            if (expectedProperty != "opname") result.Opname.Should().BeNull();
            if (expectedProperty != "others") result.Others.Should().BeNull();
        }
    }
}
