using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTemplateActivityTypeRequireItemActivityType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_templates_activity_types_activity_type_id",
                table: "budget_templates");

            migrationBuilder.DropIndex(
                name: "IX_budget_templates_activity_type_id",
                table: "budget_templates");

            // Backfill legacy item rows (created back when item-level activity type was optional
            // and only the template-level one was required) from their parent template's
            // activity_type_id before that column is dropped below.
            migrationBuilder.Sql(
                """
                UPDATE budget_template_items bti
                SET activity_type_id = bt.activity_type_id
                FROM budget_templates bt
                WHERE bti.budget_template_id = bt."Id" AND bti.activity_type_id IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE budget_plan_items bpi
                SET activity_type_id = bt.activity_type_id
                FROM budget_plans bp
                JOIN budget_templates bt ON bt."Id" = bp.budget_template_id
                WHERE bpi.budget_plan_id = bp."Id" AND bpi.activity_type_id IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "activity_type_id",
                table: "budget_templates");

            // If either ALTER below still fails with "contains null values", the backfill above
            // didn't cover every row (e.g. a plan item whose template had no activity_type_id) -
            // find and fix those rows manually, then re-run this migration.
            migrationBuilder.AlterColumn<long>(
                name: "activity_type_id",
                table: "budget_template_items",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "activity_type_id",
                table: "budget_plan_items",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "activity_type_id",
                table: "budget_templates",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<long>(
                name: "activity_type_id",
                table: "budget_template_items",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "activity_type_id",
                table: "budget_plan_items",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_activity_type_id",
                table: "budget_templates",
                column: "activity_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_templates_activity_types_activity_type_id",
                table: "budget_templates",
                column: "activity_type_id",
                principalTable: "activity_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
