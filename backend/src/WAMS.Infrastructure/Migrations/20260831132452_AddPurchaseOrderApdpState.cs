using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderApdpState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "apdp_generation_claim_token",
                table: "purchase_orders",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "apdp_generation_claimed_at",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sap_apdp_doc_entry",
                table: "purchase_orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sap_apdp_error",
                table: "purchase_orders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "sap_apdp_generated_at",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "apdp_generation_claim_token",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "apdp_generation_claimed_at",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "sap_apdp_doc_entry",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "sap_apdp_error",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "sap_apdp_generated_at",
                table: "purchase_orders");
        }
    }
}
