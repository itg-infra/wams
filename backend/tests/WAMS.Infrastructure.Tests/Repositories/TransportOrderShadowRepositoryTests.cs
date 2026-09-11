using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using WAMS.Application.DTOs.TransportOrders;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.BudgetPlans;
using WAMS.Domain.Entities.BudgetTemplates;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.TransportOrders;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.Domain.Enums;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.TransportOrders;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

public class TransportOrderShadowRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ForLoWithoutDocStatus_ReturnsAllActiveLoStatuses()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seed = new AppDbContext(options, Substitute.For<ITenantContext>()))
        {
            await seed.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var company = new Company { Code = "C1", Name = "Company", IsActive = true };
            seed.Companies.Add(company);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            seed.TransportOrderShadows.AddRange(
                NewTo(company.Id, "LO-PK", "WH-1", "LO", "PK"),
                NewTo(company.Id, "LO-PL", "WH-1", "LO", "PL"),
                NewTo(company.Id, "MO-O", "WH-1"));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            await using var db = new AppDbContext(options, Substitute.For<ITenantContext>());
            var repo = new TransportOrderShadowRepository(db);

            var (items, total) = await repo.GetAllAsync(
                new TransportOrderQuery { Type = "LO" },
                TestContext.Current.CancellationToken);

            total.Should().Be(2);
            items.Select(x => x.DocNo).Should().BeEquivalentTo("LO-PK", "LO-PL");

            var streamed = new List<TransportOrderShadowResponse>();
            await foreach (var item in repo.StreamAllAsync(
                new TransportOrderQuery { Type = "LO" },
                50,
                TestContext.Current.CancellationToken))
                streamed.Add(item);

            streamed.Select(x => x.DocNo).Should().BeEquivalentTo("LO-PK", "LO-PL");
        }

        await connection.DisposeAsync();
    }

    [Fact]
    public async Task GetAllAsync_WithBudgetPlanId_FiltersTransportOrdersToTheBudgetPlanLocation()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

        await using (var seed = new AppDbContext(options, Substitute.For<ITenantContext>()))
        {
            await seed.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

            var company = new Company { Code = "C1", Name = "Company", IsActive = true };
            seed.Companies.Add(company);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            var user = new User
            {
                CompanyId = company.Id,
                Email = "user@example.test",
                Fullname = "User",
                IsActive = true,
            };
            var bpWarehouse = new WarehouseShadow
            {
                CompanyId = company.Id,
                Code = "BP-WH",
                Name = "BP Warehouse",
                Location = "AREA-1",
                IsActive = true,
                FirstSeenAt = DateTime.UtcNow,
                SyncedAt = DateTime.UtcNow,
            };
            var otherWarehouse = new WarehouseShadow
            {
                CompanyId = company.Id,
                Code = "OTHER-WH",
                Name = "Other Warehouse",
                Location = "AREA-2",
                IsActive = true,
                FirstSeenAt = DateTime.UtcNow,
                SyncedAt = DateTime.UtcNow,
            };
            seed.Users.Add(user);
            seed.WarehouseShadows.AddRange(bpWarehouse, otherWarehouse);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            var template = new BudgetTemplate
            {
                CompanyId = company.Id,
                Code = "TPL-1",
                Status = BudgetTemplateStatus.Submitted,
                CreatedByUserId = user.Id,
            };
            seed.BudgetTemplates.Add(template);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            var bp = new BudgetPlan
            {
                CompanyId = company.Id,
                Code = "BP-1",
                BudgetTemplateId = template.Id,
                WarehouseShadowId = bpWarehouse.Id,
                CreatedByUserId = user.Id,
                DocDate = DateTime.UtcNow,
                Status = BudgetPlanStatus.Approved,
            };
            seed.BudgetPlans.Add(bp);
            seed.TransportOrderShadows.AddRange(
                NewTo(company.Id, "TO-AREA-1", bpWarehouse.Code),
                NewTo(company.Id, "TO-AREA-2", otherWarehouse.Code));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);

            await using var db = new AppDbContext(options, Substitute.For<ITenantContext>());
            var repo = new TransportOrderShadowRepository(db);
            var query = new TransportOrderQuery { BudgetPlanId = bp.Id };

            var (items, total) = await repo.GetAllAsync(query, TestContext.Current.CancellationToken);

            total.Should().Be(1);
            items.Select(x => x.DocNo).Should().Equal("TO-AREA-1");

            var streamed = new List<TransportOrderShadowResponse>();
            await foreach (var item in repo.StreamAllAsync(query, 50, TestContext.Current.CancellationToken))
                streamed.Add(item);

            streamed.Select(x => x.DocNo).Should().Equal("TO-AREA-1");
        }

        await connection.DisposeAsync();
    }

    private static TransportOrderShadow NewTo(
        long companyId,
        string docNo,
        string whsCode,
        string type = "MO",
        string docStatus = "O") => new()
    {
        CompanyId = companyId,
        DocNo = docNo,
        Type = type,
        CardCode = "V1",
        CardName = "Vendor",
        VehicleNo = docNo,
        VehicleType = "Truck",
        BlNo = "BL-1",
        ItemCode = "ITEM-1",
        ItemName = "Item",
        UoM = "KG",
        WhsCode = whsCode,
        WhsName = whsCode,
        DocStatus = docStatus,
        IsActive = true,
        FirstSeenAt = DateTime.UtcNow,
        SyncedAt = DateTime.UtcNow,
    };
}
