using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityTypeIdToTemplateAndPlanItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_budget_template_items_budget_template_id",
                table: "budget_template_items");

            migrationBuilder.AddColumn<long>(
                name: "activity_type_id",
                table: "budget_template_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "activity_type_id",
                table: "budget_plan_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_template_items_activity_type_id",
                table: "budget_template_items",
                column: "activity_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_template_items_template_item_unique",
                table: "budget_template_items",
                columns: new[] { "budget_template_id", "item_shadow_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_items_activity_type_id",
                table: "budget_plan_items",
                column: "activity_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plan_items_activity_types_activity_type_id",
                table: "budget_plan_items",
                column: "activity_type_id",
                principalTable: "activity_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_budget_template_items_activity_types_activity_type_id",
                table: "budget_template_items",
                column: "activity_type_id",
                principalTable: "activity_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_plan_items_activity_types_activity_type_id",
                table: "budget_plan_items");

            migrationBuilder.DropForeignKey(
                name: "FK_budget_template_items_activity_types_activity_type_id",
                table: "budget_template_items");

            migrationBuilder.DropIndex(
                name: "IX_budget_template_items_activity_type_id",
                table: "budget_template_items");

            migrationBuilder.DropIndex(
                name: "ix_budget_template_items_template_item_unique",
                table: "budget_template_items");

            migrationBuilder.DropIndex(
                name: "IX_budget_plan_items_activity_type_id",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "activity_type_id",
                table: "budget_template_items");

            migrationBuilder.DropColumn(
                name: "activity_type_id",
                table: "budget_plan_items");

            migrationBuilder.CreateIndex(
                name: "IX_budget_template_items_budget_template_id",
                table: "budget_template_items",
                column: "budget_template_id");
        }
    }
}
