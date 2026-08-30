using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpkShadowIdToBudgetPlanItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "spk_shadow_id",
                table: "budget_plan_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_plan_items_spk_shadow_id",
                table: "budget_plan_items",
                column: "spk_shadow_id",
                filter: "spk_shadow_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plan_items_spk_shadows_spk_shadow_id",
                table: "budget_plan_items",
                column: "spk_shadow_id",
                principalTable: "spk_shadows",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_plan_items_spk_shadows_spk_shadow_id",
                table: "budget_plan_items");

            migrationBuilder.DropIndex(
                name: "ix_budget_plan_items_spk_shadow_id",
                table: "budget_plan_items");

            migrationBuilder.DropColumn(
                name: "spk_shadow_id",
                table: "budget_plan_items");
        }
    }
}
