using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBudgetTemplateApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_templates_users_approved_by_user_id",
                table: "budget_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_budget_templates_users_rejected_by_user_id",
                table: "budget_templates");

            migrationBuilder.DropIndex(
                name: "IX_budget_templates_approved_by_user_id",
                table: "budget_templates");

            migrationBuilder.DropIndex(
                name: "IX_budget_templates_rejected_by_user_id",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "rejected_by_user_id",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "budget_templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "budget_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "approved_by_user_id",
                table: "budget_templates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "budget_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "rejected_by_user_id",
                table: "budget_templates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "budget_templates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_approved_by_user_id",
                table: "budget_templates",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_rejected_by_user_id",
                table: "budget_templates",
                column: "rejected_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_templates_users_approved_by_user_id",
                table: "budget_templates",
                column: "approved_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_budget_templates_users_rejected_by_user_id",
                table: "budget_templates",
                column: "rejected_by_user_id",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
