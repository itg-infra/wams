using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgetTemplateFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "activity_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "budget_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    activity_type_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_templates_activity_types_activity_type_id",
                        column: x => x.activity_type_id,
                        principalTable: "activity_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_templates_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_templates_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_templates_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_templates_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_budget_templates_warehouse_shadows_warehouse_shadow_id",
                        column: x => x.warehouse_shadow_id,
                        principalTable: "warehouse_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_template_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    budget_template_id = table.Column<long>(type: "bigint", nullable: false),
                    item_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_template_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_template_items_budget_templates_budget_template_id",
                        column: x => x.budget_template_id,
                        principalTable: "budget_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_budget_template_items_item_shadows_item_shadow_id",
                        column: x => x.item_shadow_id,
                        principalTable: "item_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_activity_types_code",
                table: "activity_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_budget_template_items_budget_template_id",
                table: "budget_template_items",
                column: "budget_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_template_items_item_shadow_id",
                table: "budget_template_items",
                column: "item_shadow_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_activity_type_id",
                table: "budget_templates",
                column: "activity_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_approved_by_user_id",
                table: "budget_templates",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_budget_templates_code",
                table: "budget_templates",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_templates_company_status",
                table: "budget_templates",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_created_by_user_id",
                table: "budget_templates",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_submitted_by_user_id",
                table: "budget_templates",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_templates_warehouse_shadow_id",
                table: "budget_templates",
                column: "warehouse_shadow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_template_items");

            migrationBuilder.DropTable(
                name: "budget_templates");

            migrationBuilder.DropTable(
                name: "activity_types");
        }
    }
}
