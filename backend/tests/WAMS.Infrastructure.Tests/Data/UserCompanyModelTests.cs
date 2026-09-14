using Microsoft.EntityFrameworkCore;
using WAMS.Domain.Entities.Roles;
using WAMS.Domain.Entities.Users;
using WAMS.Infrastructure.Data;
using Xunit;

namespace WAMS.Infrastructure.Tests.Data;

public class UserCompanyModelTests
{
    [Fact]
    public void UserCompany_UsesLiveUserCompanyUniqueIndex_AndCascadesAssignments()
    {
        using var db = CreateContext();
        var membership = db.Model.FindEntityType(typeof(UserCompany))!;

        Assert.NotNull(membership.FindIndex([
            membership.FindProperty(nameof(UserCompany.UserId))!,
            membership.FindProperty(nameof(UserCompany.CompanyId))!
        ]));

        Assert.Equal(DeleteBehavior.Cascade,
            membership.FindNavigation(nameof(UserCompany.Roles))!.ForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade,
            membership.FindNavigation(nameof(UserCompany.Warehouses))!.ForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade,
            membership.FindNavigation(nameof(UserCompany.Provinces))!.ForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade,
            membership.FindNavigation(nameof(UserCompany.Permissions))!.ForeignKey.DeleteBehavior);
    }

    [Fact]
    public void UserCompanyAssignments_UseMembershipAsCompositeKeyPart()
    {
        using var db = CreateContext();

        AssertMembershipKeyIncludes<UserCompanyRole>(db, nameof(UserCompanyRole.UserCompanyId));
        AssertMembershipKeyIncludes<UserCompanyWarehouse>(db, nameof(UserCompanyWarehouse.UserCompanyId));
        AssertMembershipKeyIncludes<UserCompanyProvince>(db, nameof(UserCompanyProvince.UserCompanyId));
        AssertMembershipKeyIncludes<UserCompanyPermission>(db, nameof(UserCompanyPermission.UserCompanyId));
    }

    private static void AssertMembershipKeyIncludes<TEntity>(DbContext db, string propertyName)
        where TEntity : class
    {
        var entity = db.Model.FindEntityType(typeof(TEntity))!;
        var key = entity.FindPrimaryKey()!;

        Assert.Contains(key.Properties, property => property.Name == propertyName);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(nameof(UserCompanyModelTests))
            .Options;

        return new AppDbContext(options);
    }
}
