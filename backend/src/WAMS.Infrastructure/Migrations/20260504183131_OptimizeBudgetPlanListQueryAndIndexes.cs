using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeBudgetPlanListQueryAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_budget_plan_items_budget_plan_id",
                table: "budget_plan_items");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_company_created_at",
                table: "budget_plans",
                columns: new[] { "company_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_plan_items_budget_plan_sort_order",
                table: "budget_plan_items",
                columns: new[] { "budget_plan_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_plan_items_budget_plan_vendor",
                table: "budget_plan_items",
                columns: new[] { "budget_plan_id", "vendor_shadow_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_budget_plans_company_created_at",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "ix_budget_plan_items_budget_plan_sort_order",
                table: "budget_plan_items");

            migrationBuilder.DropIndex(
                name: "ix_budget_plan_items_budget_plan_vendor",
                table: "budget_plan_items");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_items_budget_plan_id",
                table: "budget_plan_items",
                column: "budget_plan_id");
        }
    }
}
