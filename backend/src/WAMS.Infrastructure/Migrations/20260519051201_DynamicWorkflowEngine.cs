using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DynamicWorkflowEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create workflow_templates
            migrationBuilder.CreateTable(
                name: "workflow_templates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    doc_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_templates_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_templates_company_doctype",
                table: "workflow_templates",
                columns: new[] { "company_id", "doc_type" });

            // Create workflow_stages
            migrationBuilder.CreateTable(
                name: "workflow_stages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    workflow_template_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_order = table.Column<int>(type: "integer", nullable: false),
                    stage_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_roles = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_stages_workflow_templates_workflow_template_id",
                        column: x => x.workflow_template_id,
                        principalTable: "workflow_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_stages_template_order",
                table: "workflow_stages",
                columns: new[] { "workflow_template_id", "stage_order" },
                unique: true);

            // Create workflow_instances
            migrationBuilder.CreateTable(
                name: "workflow_instances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    workflow_template_id = table.Column<long>(type: "bigint", nullable: false),
                    doc_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    doc_id = table.Column<long>(type: "bigint", nullable: false),
                    current_stage_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_instances_workflow_templates_workflow_template_id",
                        column: x => x.workflow_template_id,
                        principalTable: "workflow_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instances_doctype_docid",
                table: "workflow_instances",
                columns: new[] { "doc_type", "doc_id" });

            // Create workflow_instance_stages
            migrationBuilder.CreateTable(
                name: "workflow_instance_stages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    workflow_instance_id = table.Column<long>(type: "bigint", nullable: false),
                    stage_order = table.Column<int>(type: "integer", nullable: false),
                    stage_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    approver_roles = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_instance_stages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_instance_stages_workflow_instances_workflow_instance_id",
                        column: x => x.workflow_instance_id,
                        principalTable: "workflow_instances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_instance_stages_users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workflow_instance_stages_users_rejected_by_user_id",
                        column: x => x.rejected_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_stages_instance_order",
                table: "workflow_instance_stages",
                columns: new[] { "workflow_instance_id", "stage_order" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_stages_status",
                table: "workflow_instance_stages",
                column: "status");

            // Drop old stage approval columns from budget_plans
            migrationBuilder.DropForeignKey(
                name: "FK_budget_plans_users_stage1_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropForeignKey(
                name: "FK_budget_plans_users_stage2_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "IX_budget_plans_stage1_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "IX_budget_plans_stage2_approved_by_user_id",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "ix_budget_plans_status_stage1_approved_at",
                table: "budget_plans");

            migrationBuilder.DropColumn(name: "stage1_approved_by_user_id", table: "budget_plans");
            migrationBuilder.DropColumn(name: "stage1_approved_at", table: "budget_plans");
            migrationBuilder.DropColumn(name: "stage2_approved_by_user_id", table: "budget_plans");
            migrationBuilder.DropColumn(name: "stage2_approved_at", table: "budget_plans");

            // Add workflow_instance_id to budget_plans
            migrationBuilder.AddColumn<long>(
                name: "workflow_instance_id",
                table: "budget_plans",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_workflow_instance_id",
                table: "budget_plans",
                column: "workflow_instance_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_plans_workflow_instances_workflow_instance_id",
                table: "budget_plans",
                column: "workflow_instance_id",
                principalTable: "workflow_instances",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_plans_workflow_instances_workflow_instance_id",
                table: "budget_plans");

            migrationBuilder.DropIndex(
                name: "ix_budget_plans_workflow_instance_id",
                table: "budget_plans");

            migrationBuilder.DropColumn(name: "workflow_instance_id", table: "budget_plans");

            migrationBuilder.AddColumn<long>(name: "stage1_approved_by_user_id", table: "budget_plans", type: "bigint", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "stage1_approved_at", table: "budget_plans", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<long>(name: "stage2_approved_by_user_id", table: "budget_plans", type: "bigint", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "stage2_approved_at", table: "budget_plans", type: "timestamp with time zone", nullable: true);

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

            migrationBuilder.DropTable(name: "workflow_instance_stages");
            migrationBuilder.DropTable(name: "workflow_instances");
            migrationBuilder.DropTable(name: "workflow_stages");
            migrationBuilder.DropTable(name: "workflow_templates");
        }
    }
}
