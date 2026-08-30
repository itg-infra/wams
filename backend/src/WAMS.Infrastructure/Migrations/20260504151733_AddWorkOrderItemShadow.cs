using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderItemShadow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "item_shadow_id",
                table: "work_orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_item_shadow_id",
                table: "work_orders",
                column: "item_shadow_id");

            migrationBuilder.AddForeignKey(
                name: "FK_work_orders_item_shadows_item_shadow_id",
                table: "work_orders",
                column: "item_shadow_id",
                principalTable: "item_shadows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_orders_item_shadows_item_shadow_id",
                table: "work_orders");

            migrationBuilder.DropIndex(
                name: "IX_work_orders_item_shadow_id",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "item_shadow_id",
                table: "work_orders");
        }
    }
}
