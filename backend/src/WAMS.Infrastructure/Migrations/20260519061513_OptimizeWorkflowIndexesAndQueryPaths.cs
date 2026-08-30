using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeWorkflowIndexesAndQueryPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_workflow_templates_company_doctype",
                table: "workflow_templates");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_templates_company_doctype_active",
                table: "workflow_templates",
                columns: new[] { "company_id", "doc_type", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_workflow_templates_active_per_doc",
                table: "workflow_templates",
                columns: new[] { "company_id", "doc_type" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_stages_instance_order_status",
                table: "workflow_instance_stages",
                columns: new[] { "workflow_instance_id", "stage_order", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_stages_instance_status_order",
                table: "workflow_instance_stages",
                columns: new[] { "workflow_instance_id", "status", "stage_order" });

            migrationBuilder.CreateIndex(
                name: "ix_budget_plans_status_deleted_workflow_submitted",
                table: "budget_plans",
                columns: new[] { "status", "deleted_at", "workflow_instance_id", "submitted_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_workflow_templates_company_doctype_active",
                table: "workflow_templates");

            migrationBuilder.DropIndex(
                name: "ux_workflow_templates_active_per_doc",
                table: "workflow_templates");

            migrationBuilder.DropIndex(
                name: "ix_workflow_instance_stages_instance_order_status",
                table: "workflow_instance_stages");

            migrationBuilder.DropIndex(
                name: "ix_workflow_instance_stages_instance_status_order",
                table: "workflow_instance_stages");

            migrationBuilder.DropIndex(
                name: "ix_budget_plans_status_deleted_workflow_submitted",
                table: "budget_plans");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_templates_company_doctype",
                table: "workflow_templates",
                columns: new[] { "company_id", "doc_type" });
        }
    }
}
