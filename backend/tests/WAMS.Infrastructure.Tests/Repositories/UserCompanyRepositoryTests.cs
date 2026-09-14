namespace WAMS.Infrastructure.Tests.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NSubstitute;
using WAMS.Application.Interfaces.Common;
using WAMS.Domain.Entities.Companies;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Infrastructure.Data;
using WAMS.Infrastructure.Repositories.Users;
using Xunit;

public sealed class UserCompanyRepositoryTests
{
    [Fact]
    public async Task GetLiveAsync_FindsMembershipOutsideActingTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"{nameof(UserCompanyRepositoryTests)}-{Guid.NewGuid()}")
            .Options;

        await using (var seed = new AppDbContext(options))
        {
            seed.Companies.AddRange(
                new Company { Id = 1, Code = "C1", Name = "Company 1", IsActive = true },
                new Company { Id = 2, Code = "C2", Name = "Company 2", IsActive = true });
            seed.Users.Add(new User
            {
                Id = 10,
                CompanyId = 1,
                Email = "member@example.test",
                Fullname = "Member",
                PasswordHash = "hash",
                IsActive = true
            });
            seed.UserCompanies.Add(new UserCompany { Id = 20, UserId = 10, CompanyId = 2 });
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var tenant = Substitute.For<ITenantContext>();
        tenant.IsSet.Returns(true);
        tenant.CompanyId.Returns(1);
        await using var db = new AppDbContext(options, tenant);
        var repository = new UserCompanyRepository(db);

        var membership = await repository.GetLiveAsync(10, 2, TestContext.Current.CancellationToken);

        Assert.NotNull(membership);
        Assert.Equal(20, membership.Id);
    }

    [Fact]
    public async Task ClearAssignmentsAsync_DetachesLoadedRowsBeforeMembershipIsRestored()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        db.Companies.Add(new Company { Id = 2, Code = "C2", Name = "Company 2", IsActive = true });
        db.Users.Add(new User
        {
            Id = 10,
            CompanyId = 2,
            Email = "member@example.test",
            Fullname = "Member",
            PasswordHash = "hash",
            IsActive = true
        });
        db.Roles.Add(new Role { Id = 30, Name = "FOREMAN", DisplayName = "Foreman" });
        db.UserCompanies.Add(new UserCompany
        {
            Id = 20,
            UserId = 10,
            CompanyId = 2,
            RemovedAt = DateTime.UtcNow,
            Roles = [new UserCompanyRole { RoleId = 30 }]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();

        var repository = new UserCompanyRepository(db);
        var membership = await repository.GetAnyAsync(10, 2, TestContext.Current.CancellationToken);
        Assert.NotNull(membership);
        Assert.Single(membership.Roles);

        await repository.ClearAssignmentsAsync(20, TestContext.Current.CancellationToken);
        membership.Roles.Clear();
        membership.RemovedAt = null;

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Empty(await db.UserCompanyRoles.IgnoreQueryFilters().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Null((await db.UserCompanies.IgnoreQueryFilters().SingleAsync(TestContext.Current.CancellationToken)).RemovedAt);
    }
}
