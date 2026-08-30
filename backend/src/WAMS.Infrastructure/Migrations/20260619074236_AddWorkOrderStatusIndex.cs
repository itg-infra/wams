using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CONCURRENTLY avoids taking an AccessExclusiveLock that would block reads/writes.
            // suppressTransaction: true is required - PostgreSQL forbids CONCURRENTLY inside a transaction.
            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_work_orders_status_created
                  ON work_orders (status ASC, created_at DESC, ""Id"" DESC)
                  WHERE deleted_at IS NULL;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                @"CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_budget_plans_active_created
                  ON budget_plans (created_at DESC, ""Id"" DESC)
                  WHERE deleted_at IS NULL;",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS idx_work_orders_status_created;",
                suppressTransaction: true);

            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS idx_budget_plans_active_created;",
                suppressTransaction: true);
        }
    }
}
