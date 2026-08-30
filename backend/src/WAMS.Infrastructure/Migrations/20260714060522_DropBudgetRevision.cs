using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropBudgetRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_revisions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budget_revisions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    budget_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    recap_work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revised_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_revisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_revisions_budget_plans_budget_plan_id",
                        column: x => x.budget_plan_id,
                        principalTable: "budget_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_revisions_recap_work_orders_recap_work_order_id",
                        column: x => x.recap_work_order_id,
                        principalTable: "recap_work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_revisions_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_revisions_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_budget_revisions_company_status",
                table: "budget_revisions",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_revisions_plan_status",
                table: "budget_revisions",
                columns: new[] { "budget_plan_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_revisions_reviewed_by_user_id",
                table: "budget_revisions",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_revisions_submitted_by_user_id",
                table: "budget_revisions",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "uix_budget_revisions_recap_pending",
                table: "budget_revisions",
                columns: new[] { "recap_work_order_id", "status" },
                unique: true,
                filter: "status = 'Pending'");
        }
    }
}
