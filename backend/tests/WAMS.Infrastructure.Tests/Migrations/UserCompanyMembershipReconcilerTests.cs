using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Domain.Entities.Warehouses;
using WAMS.E2eReset;
using WAMS.Infrastructure.Data;
using Xunit;

namespace WAMS.Infrastructure.Tests.Migrations;

public sealed class UserCompanyMembershipReconcilerTests
{
    [Fact]
    public async Task ReconcileAsync_ReportsOffendingMembershipAssignmentIds()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AppDbContext(options);
        var companyA = new Company { Id = 1, Code = "A", Name = "A", IsActive = true };
        var companyB = new Company { Id = 2, Code = "B", Name = "B", IsActive = true };
        var role = new Role { Id = 7, CompanyId = companyB.Id, Company = companyB, Name = "wrong-company" };
        var sharedRole = new Role { Id = 12, CompanyId = null, Name = "WH_MGR" };
        var warehouse = new WarehouseShadow
        {
            Id = 8,
            Code = "WH-B",
            Name = "Warehouse B",
            CompanyId = companyB.Id,
            Company = companyB,
            FirstSeenAt = DateTime.UtcNow,
            SyncedAt = DateTime.UtcNow
        };
        var user = new User { Id = 10, Email = "user@example.com", Fullname = "User", CompanyId = companyA.Id, Company = companyA };
        var membership = new UserCompany { Id = 11, UserId = user.Id, CompanyId = companyA.Id, User = user, Company = companyA };
        membership.Roles.Add(new UserCompanyRole { UserCompanyId = membership.Id, RoleId = role.Id, UserCompany = membership, Role = role });
        membership.Roles.Add(new UserCompanyRole { UserCompanyId = membership.Id, RoleId = sharedRole.Id, UserCompany = membership, Role = sharedRole });
        membership.Warehouses.Add(new UserCompanyWarehouse { UserCompanyId = membership.Id, WarehouseId = warehouse.Id, UserCompany = membership, Warehouse = warehouse });
        db.AddRange(companyA, companyB, role, sharedRole, warehouse, user, membership);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await new UserCompanyMembershipReconciler(db).ReconcileAsync(TestContext.Current.CancellationToken);

        result.IsHealthy.Should().BeFalse();
        result.RoleCompanyMismatches.Should().ContainSingle(value =>
            value.Contains("membership=11", StringComparison.Ordinal) &&
            value.Contains("role=7", StringComparison.Ordinal) &&
            value.Contains("roleCompany=2", StringComparison.Ordinal));
        result.RoleCompanyMismatches.Should().NotContain(value => value.Contains("role=12", StringComparison.Ordinal));
        result.WarehouseCompanyMismatches.Should().ContainSingle(value =>
            value.Contains("membership=11", StringComparison.Ordinal) &&
            value.Contains("warehouse=8", StringComparison.Ordinal) &&
            value.Contains("warehouseCompany=2", StringComparison.Ordinal));
    }
}
