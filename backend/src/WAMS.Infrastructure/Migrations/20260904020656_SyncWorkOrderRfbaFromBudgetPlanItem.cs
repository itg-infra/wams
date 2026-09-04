using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncWorkOrderRfbaFromBudgetPlanItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE work_orders AS wo
                SET is_rfba = bpi.is_rfba
                FROM budget_plan_items AS bpi
                WHERE wo.budget_plan_item_id = bpi."Id"
                  AND wo.is_rfba IS DISTINCT FROM bpi.is_rfba;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The previous values were inconsistent copies of the plan-level RFBA
            // flag and cannot be restored without reintroducing the defect.
        }
    }
}
