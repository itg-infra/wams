using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MigrateBudgetPlanStatusValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE budget_plans SET status = 'InApproval' WHERE status = 'ApprovedStage1';
                UPDATE budget_plans SET status = 'Approved'   WHERE status = 'ApprovedStage2';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE budget_plans SET status = 'ApprovedStage1' WHERE status = 'InApproval';
                UPDATE budget_plans SET status = 'ApprovedStage2' WHERE status = 'Approved';
                """);
        }
    }
}
