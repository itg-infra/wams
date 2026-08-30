using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostTreatment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cost_treatment",
                table: "rate_card_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cost_treatment",
                table: "purchase_order_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cost_treatment",
                table: "budget_plan_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cost_treatment",
                table: "rate_card_items");

            migrationBuilder.DropColumn(
                name: "cost_treatment",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "cost_treatment",
                table: "budget_plan_items");
        }
    }
}
