using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToBudgetTemplateAndWarehouseToBudgetPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add location column to budget_templates (nullable - backfill next)
            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "budget_templates",
                type: "text",
                nullable: true);

            // Step 2: Backfill location from the warehouse the template was previously linked to
            migrationBuilder.Sql("""
                UPDATE budget_templates bt
                SET location = ws.location
                FROM warehouse_shadows ws
                WHERE ws."Id" = bt.warehouse_shadow_id;
                """);

            // Step 3: Add warehouse_shadow_id to budget_plans as nullable first (backfill next)
            migrationBuilder.AddColumn<long>(
                name: "warehouse_shadow_id",
                table: "budget_plans",
                type: "bigint",
                nullable: true);

            // Step 4: Backfill warehouse from the budget plan's linked template
            migrationBuilder.Sql("""
                UPDATE budget_plans bp
                SET warehouse_shadow_id = bt.warehouse_shadow_id
                FROM budget_templates bt
                WHERE bt."Id" = bp.budget_template_id;
                """);

            // Step 5: Make warehouse_shadow_id NOT NULL now that it's populated
            migrationBuilder.AlterColumn<long>(
                name: "warehouse_shadow_id",
                table: "budget_plans",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            // Step 6: Add FK + index on budget_plans.warehouse_shadow_id
            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_warehouse_shadow_id",
                table: "budget_plans",
                column: "warehouse_shadow_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plans_warehouse_shadows_warehouse_shadow_id",
                table: "budget_plans",
                column: "warehouse_shadow_id",
                principalTable: "warehouse_shadows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Step 7: Drop warehouse_shadow_id from budget_templates (data safely migrated)
            migrationBuilder.DropForeignKey(
                name: "FK_budget_templates_warehouse_shadows_warehouse_shadow_id",
                table: "budget_templates");

            migrationBuilder.DropIndex(
                name: "idx_budget_templates_warehouse_shadow_id",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "warehouse_shadow_id",
                table: "budget_templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration moves WarehouseShadowId from budget_templates to budget_plans and
            // replaces it with a denormalized location string. Reversing it is not safe in production:
            // any template whose budget plans were deleted after Up() ran has no recoverable warehouse mapping.
            throw new InvalidOperationException(
                "Migration AddLocationToBudgetTemplateAndWarehouseToBudgetPlan cannot be rolled back safely. " +
                "Reverse the schema change manually if required.");
        }
    }
}
