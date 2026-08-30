using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BudgetPlanTwoStageApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_plans_users_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.RenameColumn(
                name: "approved_by_user_id",
                table: "budget_plans",
                newName: "stage2_approved_by_user_id");

            migrationBuilder.RenameColumn(
                name: "approved_at",
                table: "budget_plans",
                newName: "stage2_approved_at");

            migrationBuilder.RenameIndex(
                name: "IX_budget_plans_approved_by_user_id",
                table: "budget_plans",
                newName: "IX_budget_plans_stage2_approved_by_user_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "stage1_approved_at",
                table: "budget_plans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "stage1_approved_by_user_id",
                table: "budget_plans",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_plans_stage1_approved_by_user_id",
                table: "budget_plans",
                column: "stage1_approved_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plans_users_stage1_approved_by_user_id",
                table: "budget_plans",
                column: "stage1_approved_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plans_users_stage2_approved_by_user_id",
                table: "budget_plans",
                column: "stage2_approved_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_plans_users_stage1_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_budget_plans_users_stage2_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "IX_budget_plans_stage1_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "stage1_approved_at",
                table: "budget_plans");

            migrationBuilder.DropColumn(
                name: "stage1_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.RenameColumn(
                name: "stage2_approved_by_user_id",
                table: "budget_plans",
                newName: "approved_by_user_id");

            migrationBuilder.RenameColumn(
                name: "stage2_approved_at",
                table: "budget_plans",
                newName: "approved_at");

            migrationBuilder.RenameIndex(
                name: "IX_budget_plans_stage2_approved_by_user_id",
                table: "budget_plans",
                newName: "IX_budget_plans_approved_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plans_users_approved_by_user_id",
                table: "budget_plans",
                column: "approved_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
