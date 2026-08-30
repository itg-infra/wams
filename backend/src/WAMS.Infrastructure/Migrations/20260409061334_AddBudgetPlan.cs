using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budget_plans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    budget_template_id = table.Column<long>(type: "bigint", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    doc_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_plans_budget_templates_budget_template_id",
                        column: x => x.budget_template_id,
                        principalTable: "budget_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plans_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plans_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plans_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plans_users_rejected_by_user_id",
                        column: x => x.rejected_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plans_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_plan_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    budget_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    item_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    uom_master_id = table.Column<long>(type: "bigint", nullable: false),
                    cost_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_plan_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_plan_items_budget_plans_budget_plan_id",
                        column: x => x.budget_plan_id,
                        principalTable: "budget_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_budget_plan_items_item_shadows_item_shadow_id",
                        column: x => x.item_shadow_id,
                        principalTable: "item_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plan_items_uom_masters_uom_master_id",
                        column: x => x.uom_master_id,
                        principalTable: "uom_masters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_plan_items_vendor_shadows_vendor_shadow_id",
                        column: x => x.vendor_shadow_id,
                        principalTable: "vendor_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_items_budget_plan_id",
                table: "budget_plan_items",
                column: "budget_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_items_item_shadow_id",
                table: "budget_plan_items",
                column: "item_shadow_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_items_uom_master_id",
                table: "budget_plan_items",
                column: "uom_master_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_items_vendor_shadow_id",
                table: "budget_plan_items",
                column: "vendor_shadow_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plans_approved_by_user_id",
                table: "budget_plans",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plans_budget_template_id",
                table: "budget_plans",
                column: "budget_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_code",
                table: "budget_plans",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_company_status",
                table: "budget_plans",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_plans_created_by_user_id",
                table: "budget_plans",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plans_rejected_by_user_id",
                table: "budget_plans",
                column: "rejected_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plans_submitted_by_user_id",
                table: "budget_plans",
                column: "submitted_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_plan_items");

            migrationBuilder.DropTable(
                name: "budget_plans");
        }
    }
}
