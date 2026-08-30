using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderItemIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_purchase_order_items_purchase_order_id",
                table: "purchase_order_items",
                newName: "ix_purchase_order_items_purchase_order_id");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_order_items_budget_plan_item_id",
                table: "purchase_order_items",
                newName: "ix_purchase_order_items_budget_plan_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_purchase_order_items_purchase_order_id",
                table: "purchase_order_items",
                newName: "IX_purchase_order_items_purchase_order_id");

            migrationBuilder.RenameIndex(
                name: "ix_purchase_order_items_budget_plan_item_id",
                table: "purchase_order_items",
                newName: "IX_purchase_order_items_budget_plan_item_id");
        }
    }
}
