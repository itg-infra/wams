using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_work_orders_warehouse_shadow_id",
                table: "work_orders");

            migrationBuilder.CreateIndex(
                name: "idx_work_orders_active_created",
                table: "work_orders",
                columns: new[] { "created_at", "Id" },
                descending: new bool[0],
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_work_orders_active_created_date",
                table: "work_orders",
                columns: new[] { "created_at", "warehouse_shadow_id" },
                descending: new[] { true, false },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_work_orders_active_warehouse_created",
                table: "work_orders",
                columns: new[] { "warehouse_shadow_id", "created_at", "Id" },
                descending: new[] { false, true, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_budget_plan_spk_items_plan_sort",
                table: "budget_plan_spk_items",
                columns: new[] { "budget_plan_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_work_orders_active_created",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "idx_work_orders_active_created_date",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "idx_work_orders_active_warehouse_created",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "idx_budget_plan_spk_items_plan_sort",
                table: "budget_plan_spk_items");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_warehouse_shadow_id",
                table: "work_orders",
                column: "warehouse_shadow_id");
        }
    }
}
