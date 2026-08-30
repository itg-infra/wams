using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportOrderShadow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_order_transport_orders_budget_plans_budget_plan_id",
                table: "work_order_transport_orders");

            migrationBuilder.RenameColumn(
                name: "budget_plan_id",
                table: "work_order_transport_orders",
                newName: "transport_order_shadow_id");

            migrationBuilder.RenameIndex(
                name: "IX_work_order_transport_orders_budget_plan_id",
                table: "work_order_transport_orders",
                newName: "IX_work_order_transport_orders_transport_order_shadow_id");

            migrationBuilder.CreateTable(
                name: "transport_order_shadows",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    doc_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    doc_date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    card_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    card_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    vehicle_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    bl_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    container_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    seal_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    whs_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    whs_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    doc_status = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transport_order_shadows", x => x.id);
                    table.ForeignKey(
                        name: "FK_transport_order_shadows_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transport_order_shadows_filter",
                table: "transport_order_shadows",
                columns: new[] { "company_id", "type", "doc_status", "whs_code" });

            migrationBuilder.CreateIndex(
                name: "ux_transport_order_shadows_company_docno_blno",
                table: "transport_order_shadows",
                columns: new[] { "company_id", "doc_no", "bl_no" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_work_order_transport_orders_transport_order_shadows_transpo~",
                table: "work_order_transport_orders",
                column: "transport_order_shadow_id",
                principalTable: "transport_order_shadows",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_order_transport_orders_transport_order_shadows_transpo~",
                table: "work_order_transport_orders");

            migrationBuilder.DropTable(
                name: "transport_order_shadows");

            migrationBuilder.RenameColumn(
                name: "transport_order_shadow_id",
                table: "work_order_transport_orders",
                newName: "budget_plan_id");

            migrationBuilder.RenameIndex(
                name: "IX_work_order_transport_orders_transport_order_shadow_id",
                table: "work_order_transport_orders",
                newName: "IX_work_order_transport_orders_budget_plan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_work_order_transport_orders_budget_plans_budget_plan_id",
                table: "work_order_transport_orders",
                column: "budget_plan_id",
                principalTable: "budget_plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
