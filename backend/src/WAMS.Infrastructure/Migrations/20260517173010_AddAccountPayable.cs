using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountPayable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_payables",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    remark = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    doc_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sap_ap_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    generated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_payables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_payables_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_payables_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_payables_users_generated_by_user_id",
                        column: x => x.generated_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_payables_vendor_shadows_vendor_shadow_id",
                        column: x => x.vendor_shadow_id,
                        principalTable: "vendor_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_payable_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    account_payable_id = table.Column<long>(type: "bigint", nullable: false),
                    budget_plan_item_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    coa_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    coa_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    uom_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    uom_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_rfba = table.Column<bool>(type: "boolean", nullable: false),
                    bill_of_lading = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    unit_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_count = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    budget_plan_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    budget_realization = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    budget_variance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_payable_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_payable_items_account_payables_account_payable_id",
                        column: x => x.account_payable_id,
                        principalTable: "account_payables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_payable_items_budget_plan_items_budget_plan_item_id",
                        column: x => x.budget_plan_item_id,
                        principalTable: "budget_plan_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_payable_items_account_payable_id",
                table: "account_payable_items",
                column: "account_payable_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_payable_items_budget_plan_item_id",
                table: "account_payable_items",
                column: "budget_plan_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_payables_code",
                table: "account_payables",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_payables_company_status",
                table: "account_payables",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_account_payables_created_by_user_id",
                table: "account_payables",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_payables_doc_date",
                table: "account_payables",
                column: "doc_date");

            migrationBuilder.CreateIndex(
                name: "IX_account_payables_generated_by_user_id",
                table: "account_payables",
                column: "generated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_payables_vendor_shadow_id",
                table: "account_payables",
                column: "vendor_shadow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_payable_items");

            migrationBuilder.DropTable(
                name: "account_payables");
        }
    }
}
