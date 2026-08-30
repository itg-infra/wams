using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStatusToPurchaseOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "purchase_order_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unpaid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "purchase_order_items");
        }
    }
}
