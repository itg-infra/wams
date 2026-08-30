using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecapWorkOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_budget_templates_warehouse_shadow_id",
                table: "budget_templates",
                newName: "idx_budget_templates_warehouse_shadow_id");

            migrationBuilder.RenameIndex(
                name: "IX_budget_plan_spk_items_budget_plan_id",
                table: "budget_plan_spk_items",
                newName: "idx_budget_plan_spk_items_budget_plan_id");

            migrationBuilder.CreateTable(
                name: "recap_work_orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    budget_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recap_work_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_recap_work_orders_budget_plans_budget_plan_id",
                        column: x => x.budget_plan_id,
                        principalTable: "budget_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recap_work_orders_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_recap_work_orders_company_status",
                table: "recap_work_orders",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_recap_work_orders_budget_plan_id",
                table: "recap_work_orders",
                column: "budget_plan_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recap_work_orders_reviewed_by_user_id",
                table: "recap_work_orders",
                column: "reviewed_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "recap_work_orders");

            migrationBuilder.RenameIndex(
                name: "idx_budget_templates_warehouse_shadow_id",
                table: "budget_templates",
                newName: "IX_budget_templates_warehouse_shadow_id");

            migrationBuilder.RenameIndex(
                name: "idx_budget_plan_spk_items_budget_plan_id",
                table: "budget_plan_spk_items",
                newName: "IX_budget_plan_spk_items_budget_plan_id");
        }
    }
}
