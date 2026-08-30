using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPlanSubmittedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_budget_plans_active_submitted
                  ON budget_plans (submitted_at DESC, ""Id"" DESC)
                  WHERE deleted_at IS NULL;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS idx_budget_plans_active_submitted;",
                suppressTransaction: true);
        }
    }
}
