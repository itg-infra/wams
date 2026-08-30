using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPlanItemIdToWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "budget_plan_item_id",
                table: "work_orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uix_work_orders_budget_plan_item_active",
                table: "work_orders",
                column: "budget_plan_item_id",
                unique: true,
                filter: "deleted_at IS NULL AND budget_plan_item_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_budget_plan_items_budget_plan_item_id",
                table: "work_orders",
                column: "budget_plan_item_id",
                principalTable: "budget_plan_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_budget_plan_items_budget_plan_item_id",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "uix_work_orders_budget_plan_item_active",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "budget_plan_item_id",
                table: "work_orders");
        }
    }
}
