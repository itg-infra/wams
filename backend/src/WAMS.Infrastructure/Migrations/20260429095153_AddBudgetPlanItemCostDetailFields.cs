using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPlanItemCostDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bill_of_lading",
                table: "budget_plan_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "budget_plan_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "doc_external",
                table: "budget_plan_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bill_of_lading",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "description",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "doc_external",
                table: "budget_plan_items");
        }
    }
}
