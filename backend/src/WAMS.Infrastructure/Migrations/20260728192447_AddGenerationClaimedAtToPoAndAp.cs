using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationClaimedAtToPoAndAp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "generation_claimed_at",
                table: "purchase_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "generation_claimed_at",
                table: "account_payables",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "generation_claimed_at",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "generation_claimed_at",
                table: "account_payables");
        }
    }
}
