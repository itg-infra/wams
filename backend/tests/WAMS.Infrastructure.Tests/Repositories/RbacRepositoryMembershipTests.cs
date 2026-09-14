using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.Rbac;
using NSubstitute;
using Xunit;

namespace WAMS.Infrastructure.Tests.Repositories;

public sealed class RbacRepositoryMembershipTests
{
    [Fact]
    public async Task ExpiredMembershipRole_IsExcludedFromRbacSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var tenant = Substitute.For<ITenantContext>();
        tenant.IsSet.Returns(false);

        await using (var seed = new AppDbContext(options, tenant))
        {
            var company = new Company { Id = 2, Code = "ACT", Name = "Acting", IsActive = true };
            var permission = new Permission { Id = 10, Module = "user", Resource = "user", Action = "read" };
            var role = new Role { Id = 20, CompanyId = company.Id, Company = company, Name = "viewer", DisplayName = "Viewer" };
            role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission });
            var user = new User { Id = 30, CompanyId = company.Id, Company = company, Email = "user@example.com", Fullname = "User" };
            var membership = new UserCompany { Id = 40, UserId = user.Id, CompanyId = company.Id, User = user, Company = company };
            membership.Roles.Add(new UserCompanyRole
            {
                UserCompanyId = membership.Id,
                RoleId = role.Id,
                UserCompany = membership,
                Role = role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
            });

            seed.AddRange(company, permission, role, user, membership);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = new AppDbContext(options, tenant);
        var repository = new RbacRepository(db);

        var snapshot = await repository.GetUserRbacSnapshotAsync(
            30,
            40,
            TestContext.Current.CancellationToken);

        snapshot.RolePermissionKeys.Should().BeEmpty();
        snapshot.HasGlobalAccess.Should().BeFalse();
    }
}
