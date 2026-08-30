using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApTaxSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cost_treatment",
                table: "account_payable_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "grand_total",
                table: "account_payable_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pph_amount",
                table: "account_payable_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pph_rate",
                table: "account_payable_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "pph_tax_type_code",
                table: "account_payable_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_amount",
                table: "account_payable_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_rate",
                table: "account_payable_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ppn_tax_type_code",
                table: "account_payable_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_treatment",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "grand_total",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "pph_amount",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "pph_rate",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "pph_tax_type_code",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "ppn_amount",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "ppn_rate",
                table: "account_payable_items");

            migrationBuilder.DropColumn(
                name: "ppn_tax_type_code",
                table: "account_payable_items");
        }
    }
}
