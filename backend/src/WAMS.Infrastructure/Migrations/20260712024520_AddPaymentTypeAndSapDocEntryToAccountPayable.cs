using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTypeAndSapDocEntryToAccountPayable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sap_apdp_doc_entry",
                table: "account_payables",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sap_doc_entry",
                table: "account_payables",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_type",
                table: "account_payable_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NoAdvance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sap_apdp_doc_entry",
                table: "account_payables");

            migrationBuilder.DropColumn(
                name: "sap_doc_entry",
                table: "account_payables");

            migrationBuilder.DropColumn(
                name: "payment_type",
                table: "account_payable_items");
        }
    }
}
