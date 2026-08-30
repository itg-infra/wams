using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MovePlanTypeIsRfbaPaymentTypeToItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_rfba",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "payment_type",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "type",
                table: "budget_plans");

            migrationBuilder.AddColumn<bool>(
                name: "is_rfba",
                table: "budget_plan_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "payment_type",
                table: "budget_plan_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "budget_plan_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_rfba",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "payment_type",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "type",
                table: "budget_plan_items");

            migrationBuilder.AddColumn<bool>(
                name: "is_rfba",
                table: "budget_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "payment_type",
                table: "budget_plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "budget_plans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }
    }
}
