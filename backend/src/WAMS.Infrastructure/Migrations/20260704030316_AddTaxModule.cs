using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "pph_rate",
                table: "rate_card_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pph_tax_type_code",
                table: "rate_card_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "pph_tax_type_id",
                table: "rate_card_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_rate",
                table: "rate_card_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ppn_tax_type_code",
                table: "rate_card_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ppn_tax_type_id",
                table: "rate_card_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "grand_total",
                table: "purchase_order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pph_amount",
                table: "purchase_order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pph_rate",
                table: "purchase_order_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "pph_tax_type_code",
                table: "purchase_order_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_amount",
                table: "purchase_order_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_rate",
                table: "purchase_order_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ppn_tax_type_code",
                table: "purchase_order_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "grand_total",
                table: "budget_plan_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pph_amount",
                table: "budget_plan_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "pph_rate",
                table: "budget_plan_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "pph_tax_type_code",
                table: "budget_plan_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_amount",
                table: "budget_plan_items",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ppn_rate",
                table: "budget_plan_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ppn_tax_type_code",
                table: "budget_plan_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tax_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    category = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_types", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "tax_types",
                columns: new[] { "Id", "category", "code", "created_at", "is_active", "name", "rate", "updated_at" },
                values: new object[,]
                {
                    { 1L, "Ppn", "PPN0", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, "No PPN", 0.00m, null },
                    { 2L, "Ppn", "PPN11", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, "PPN 11%", 11.00m, null },
                    { 3L, "Pph", "PPH22", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, "PPh 22 (Barang)", 1.50m, null },
                    { 4L, "Pph", "PPH23", new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, "PPh 23 (Jasa)", 2.00m, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_tax_types_code",
                table: "tax_types",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tax_types");

            migrationBuilder.DropColumn(
                name: "pph_rate",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "pph_tax_type_code",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "pph_tax_type_id",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "ppn_rate",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "ppn_tax_type_code",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "ppn_tax_type_id",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "grand_total",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "pph_amount",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "pph_rate",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "pph_tax_type_code",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "ppn_amount",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "ppn_rate",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "ppn_tax_type_code",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "grand_total",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "pph_amount",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "pph_rate",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "pph_tax_type_code",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "ppn_amount",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "ppn_rate",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "ppn_tax_type_code",
                table: "budget_plan_items");
        }
    }
}
