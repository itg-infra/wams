using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_orders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    budget_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    activity_type_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    warehouse_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    template_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    code_block = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    pic_user_id = table.Column<long>(type: "bigint", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_rfba = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_orders_budget_plans_budget_plan_id",
                        column: x => x.budget_plan_id,
                        principalTable: "budget_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_users_pic_user_id",
                        column: x => x.pic_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_orders_warehouse_shadows_warehouse_shadow_id",
                        column: x => x.warehouse_shadow_id,
                        principalTable: "warehouse_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_fumigation_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    fumi_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_duration = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    bl_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mv_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    initial_temperature = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    final_temperature = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    fumigation_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    methyl_bromide_dosage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    sulphur_fluoride_dosage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    phosphine_dosage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    result = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_fumigation_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_fumigation_details_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_heavy_equip_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    bl_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    standby_duration1 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    standby_duration2 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    minimum_duration = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cost_per_hour = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_heavy_equip_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_heavy_equip_details_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_loading_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    spk_shadow_id = table.Column<long>(type: "bigint", nullable: true),
                    bl_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    no_vehicle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_seal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gross_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    final_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    nett_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_bag = table.Column<int>(type: "integer", nullable: true),
                    unit_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_checked = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_loading_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_loading_items_spk_shadows_spk_shadow_id",
                        column: x => x.spk_shadow_id,
                        principalTable: "spk_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_order_loading_items_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_qc_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    moisture_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    jamur_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    bau_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    quality_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_qc_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_qc_details_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_rebagging_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    receiver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    no_vehicle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_seal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    initial_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    final_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_rebagging_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_rebagging_details_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_storage_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    has_pindah_stapel = table.Column<bool>(type: "boolean", nullable: false),
                    has_pembersihan = table.Column<bool>(type: "boolean", nullable: false),
                    has_perapihan = table.Column<bool>(type: "boolean", nullable: false),
                    volume_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    worker_on_duty = table.Column<int>(type: "integer", nullable: true),
                    has_mask = table.Column<bool>(type: "boolean", nullable: false),
                    has_safety_glasses = table.Column<bool>(type: "boolean", nullable: false),
                    has_hand_gloves = table.Column<bool>(type: "boolean", nullable: false),
                    has_helmet = table.Column<bool>(type: "boolean", nullable: false),
                    has_safety_shoes = table.Column<bool>(type: "boolean", nullable: false),
                    has_safety_vest = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_storage_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_storage_details_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_unbagging_details",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    no_vehicle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_seal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    initial_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    final_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    unit_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_bag = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_unbagging_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_unbagging_details_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_order_unloading_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    work_order_id = table.Column<long>(type: "bigint", nullable: false),
                    spk_shadow_id = table.Column<long>(type: "bigint", nullable: true),
                    bl_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    uom_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    no_vehicle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    no_seal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gross_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    final_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    nett_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_bag = table.Column<int>(type: "integer", nullable: true),
                    unit_weight = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    is_checked = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_order_unloading_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_order_unloading_items_spk_shadows_spk_shadow_id",
                        column: x => x.spk_shadow_id,
                        principalTable: "spk_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_order_unloading_items_work_orders_work_order_id",
                        column: x => x.work_order_id,
                        principalTable: "work_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_order_fumigation_details_work_order_id",
                table: "work_order_fumigation_details",
                column: "work_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_heavy_equip_details_work_order_id",
                table: "work_order_heavy_equip_details",
                column: "work_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_loading_items_spk_shadow_id",
                table: "work_order_loading_items",
                column: "spk_shadow_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_loading_items_work_order_id",
                table: "work_order_loading_items",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_qc_details_work_order_id",
                table: "work_order_qc_details",
                column: "work_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_rebagging_details_work_order_id",
                table: "work_order_rebagging_details",
                column: "work_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_storage_details_work_order_id",
                table: "work_order_storage_details",
                column: "work_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_unbagging_details_work_order_id",
                table: "work_order_unbagging_details",
                column: "work_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_order_unloading_items_spk_shadow_id",
                table: "work_order_unloading_items",
                column: "spk_shadow_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_order_unloading_items_work_order_id",
                table: "work_order_unloading_items",
                column: "work_order_id");

            migrationBuilder.CreateIndex(
                name: "idx_work_orders_budget_plan_id",
                table: "work_orders",
                column: "budget_plan_id");

            migrationBuilder.CreateIndex(
                name: "idx_work_orders_company_status",
                table: "work_orders",
                columns: new[] { "company_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_work_orders_code",
                table: "work_orders",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_created_by_user_id",
                table: "work_orders",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_pic_user_id",
                table: "work_orders",
                column: "pic_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_submitted_by_user_id",
                table: "work_orders",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_orders_warehouse_shadow_id",
                table: "work_orders",
                column: "warehouse_shadow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_order_fumigation_details");

            migrationBuilder.DropTable(
                name: "work_order_heavy_equip_details");

            migrationBuilder.DropTable(
                name: "work_order_loading_items");

            migrationBuilder.DropTable(
                name: "work_order_qc_details");

            migrationBuilder.DropTable(
                name: "work_order_rebagging_details");

            migrationBuilder.DropTable(
                name: "work_order_storage_details");

            migrationBuilder.DropTable(
                name: "work_order_unbagging_details");

            migrationBuilder.DropTable(
                name: "work_order_unloading_items");

            migrationBuilder.DropTable(
                name: "work_orders");
        }
    }
}
