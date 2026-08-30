using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBpReminderIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notifications_type_reference_created_at",
                table: "notifications",
                columns: new[] { "type", "reference_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_status_stage1_approved_at",
                table: "budget_plans",
                columns: new[] { "status", "stage1_approved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_status_submitted_at",
                table: "budget_plans",
                columns: new[] { "status", "submitted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_type_reference_created_at",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "ix_budget_plans_status_stage1_approved_at",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "ix_budget_plans_status_submitted_at",
                table: "budget_plans");
        }
    }
}
