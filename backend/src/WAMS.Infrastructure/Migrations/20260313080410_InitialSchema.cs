using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "uom_masters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uom_masters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "item_shadows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    item_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    acct_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    acct_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_shadows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_item_shadows_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    global_access = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roles_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    Fullname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    employee_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_users_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vendor_shadows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    card_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    card_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_shadows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_shadows_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_shadows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_shadows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_warehouse_shadows_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    granted_by = table.Column<long>(type: "bigint", nullable: true),
                    constraints = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    device_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_permissions",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    permission_id = table.Column<long>(type: "bigint", nullable: false),
                    is_granted = table.Column<bool>(type: "boolean", nullable: false),
                    granted_by = table.Column<long>(type: "bigint", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    constraints = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_permissions", x => new { x.user_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_user_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_granted_by",
                        column: x => x.granted_by,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rate_cards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    vendor_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rate_cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rate_cards_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rate_cards_vendor_shadows_vendor_shadow_id",
                        column: x => x.vendor_shadow_id,
                        principalTable: "vendor_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_warehouses",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_warehouses", x => new { x.user_id, x.warehouse_id });
                    table.ForeignKey(
                        name: "FK_user_warehouses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_warehouses_warehouse_shadows_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouse_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rate_card_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    rate_card_id = table.Column<long>(type: "bigint", nullable: false),
                    item_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    uom_master_id = table.Column<long>(type: "bigint", nullable: false),
                    cost_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rate_card_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rate_card_items_item_shadows_item_shadow_id",
                        column: x => x.item_shadow_id,
                        principalTable: "item_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rate_card_items_rate_cards_rate_card_id",
                        column: x => x.rate_card_id,
                        principalTable: "rate_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rate_card_items_uom_masters_uom_master_id",
                        column: x => x.uom_master_id,
                        principalTable: "uom_masters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_companies_Code",
                table: "companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_item_shadows_company_id",
                table: "item_shadows",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_item_shadows_company_id_item_code",
                table: "item_shadows",
                columns: new[] { "company_id", "item_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_permissions_module_resource_action",
                table: "permissions",
                columns: new[] { "Module", "Resource", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rate_card_items_item_shadow_id",
                table: "rate_card_items",
                column: "item_shadow_id");

            migrationBuilder.CreateIndex(
                name: "IX_rate_card_items_rate_card_id",
                table: "rate_card_items",
                column: "rate_card_id");

            migrationBuilder.CreateIndex(
                name: "IX_rate_card_items_uom_master_id",
                table: "rate_card_items",
                column: "uom_master_id");

            migrationBuilder.CreateIndex(
                name: "idx_rate_cards_company_id",
                table: "rate_cards",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_rate_cards_status",
                table: "rate_cards",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_rate_cards_created_by_user_id",
                table: "rate_cards",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_rate_cards_vendor_shadow_id",
                table: "rate_cards",
                column: "vendor_shadow_id");

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "idx_roles_name",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_CompanyId",
                table: "roles",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_uom_masters_code",
                table: "uom_masters",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_permissions_expires_at",
                table: "user_permissions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_permissions_user_granted",
                table: "user_permissions",
                columns: new[] { "user_id", "is_granted" });

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_granted_by",
                table: "user_permissions",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_permissions_permission_id",
                table: "user_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_warehouses_primary",
                table: "user_warehouses",
                columns: new[] { "user_id", "is_primary" },
                unique: true,
                filter: "\"is_primary\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_user_warehouses_warehouse_id",
                table: "user_warehouses",
                column: "warehouse_id");

            migrationBuilder.CreateIndex(
                name: "idx_users_company_id",
                table: "users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "Email",
                unique: true,
                filter: "\"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_users_is_active",
                table: "users",
                columns: new[] { "is_active", "deleted_at" },
                filter: "\"is_active\" = true AND \"deleted_at\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "idx_vendor_shadows_company_id",
                table: "vendor_shadows",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_shadows_company_id_card_code",
                table: "vendor_shadows",
                columns: new[] { "company_id", "card_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_shadows_company_id",
                table: "warehouse_shadows",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_shadows_company_id_code",
                table: "warehouse_shadows",
                columns: new[] { "CompanyId", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rate_card_items");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_warehouses");

            migrationBuilder.DropTable(
                name: "item_shadows");

            migrationBuilder.DropTable(
                name: "rate_cards");

            migrationBuilder.DropTable(
                name: "uom_masters");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "warehouse_shadows");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "vendor_shadows");

            migrationBuilder.DropTable(
                name: "companies");
        }
    }
}
