using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using WAMS.Infrastructure.Migrations;
using Xunit;

namespace WAMS.Infrastructure.Tests.Migrations;

public class UserCompanyBackfillTests
{
    [Fact]
    public void AddUserCompanyMemberships_ContainsIdempotentBackfillAndReconciliationSql()
    {
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var up = typeof(AddUserCompanyMemberships).GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        up.Invoke(new AddUserCompanyMemberships(), [migrationBuilder]);

        var sql = string.Join(
            Environment.NewLine,
            migrationBuilder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        Assert.Contains("INSERT INTO user_companies", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INSERT INTO user_company_roles", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INSERT INTO user_company_warehouses", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INSERT INTO user_company_provinces", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INSERT INTO user_company_permissions", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ON CONFLICT DO NOTHING", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SUPER_ADMIN", sql, StringComparison.Ordinal);
        Assert.Contains("RAISE EXCEPTION", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reconciliation", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("u.\"CreatedBy\"", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("u.\"CreatedAt\"", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("u.\"DeletedAt\"", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("u.created_at", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("u.deleted_at", sql, StringComparison.OrdinalIgnoreCase);
    }
}
