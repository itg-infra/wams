using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSPKTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "spk_shadows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    doc_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    base_doc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    base_doc_no = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    card_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    card_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    delivery_qty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    pack_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    whs_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    whs_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    doc_status = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    bl_no = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spk_shadows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_spk_shadows_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "budget_plan_spk_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    budget_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    spk_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_plan_spk_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_plan_spk_items_budget_plans_budget_plan_id",
                        column: x => x.budget_plan_id,
                        principalTable: "budget_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_budget_plan_spk_items_spk_shadows_spk_shadow_id",
                        column: x => x.spk_shadow_id,
                        principalTable: "spk_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_spk_items_budget_plan_id",
                table: "budget_plan_spk_items",
                column: "budget_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_budget_plan_spk_items_spk_shadow_id",
                table: "budget_plan_spk_items",
                column: "spk_shadow_id");

            migrationBuilder.CreateIndex(
                name: "ix_spk_shadows_company_doc_no",
                table: "spk_shadows",
                columns: new[] { "company_id", "doc_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_spk_shadows_company_whs",
                table: "spk_shadows",
                columns: new[] { "company_id", "whs_code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budget_plan_spk_items");

            migrationBuilder.DropTable(
                name: "spk_shadows");
        }
    }
}
