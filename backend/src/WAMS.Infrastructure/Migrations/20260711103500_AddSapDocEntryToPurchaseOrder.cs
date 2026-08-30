using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSapDocEntryToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sap_doc_entry",
                table: "purchase_orders",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sap_doc_entry",
                table: "purchase_orders");
        }
    }
}
