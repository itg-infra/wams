using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCompanyMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_companies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<long>(type: "bigint", nullable: false),
                    authorization_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by = table.Column<long>(type: "bigint", nullable: true),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_companies_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_companies_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_company_permissions",
                columns: table => new
                {
                    user_company_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_user_company_permissions", x => new { x.user_company_id, x.permission_id });
                    table.ForeignKey(
                        name: "FK_user_company_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_company_permissions_user_companies_user_company_id",
                        column: x => x.user_company_id,
                        principalTable: "user_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_company_permissions_users_granted_by",
                        column: x => x.granted_by,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_company_provinces",
                columns: table => new
                {
                    user_company_id = table.Column<long>(type: "bigint", nullable: false),
                    province_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_company_provinces", x => new { x.user_company_id, x.province_id });
                    table.ForeignKey(
                        name: "FK_user_company_provinces_provinces_province_id",
                        column: x => x.province_id,
                        principalTable: "provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_company_provinces_user_companies_user_company_id",
                        column: x => x.user_company_id,
                        principalTable: "user_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_company_roles",
                columns: table => new
                {
                    user_company_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_company_roles", x => new { x.user_company_id, x.role_id });
                    table.ForeignKey(
                        name: "FK_user_company_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_company_roles_user_companies_user_company_id",
                        column: x => x.user_company_id,
                        principalTable: "user_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_company_warehouses",
                columns: table => new
                {
                    user_company_id = table.Column<long>(type: "bigint", nullable: false),
                    warehouse_id = table.Column<long>(type: "bigint", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_company_warehouses", x => new { x.user_company_id, x.warehouse_id });
                    table.ForeignKey(
                        name: "FK_user_company_warehouses_user_companies_user_company_id",
                        column: x => x.user_company_id,
                        principalTable: "user_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_company_warehouses_warehouse_shadows_warehouse_id",
                        column: x => x.warehouse_id,
                        principalTable: "warehouse_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_user_companies_user_company_live",
                table: "user_companies",
                columns: new[] { "user_id", "company_id" },
                unique: true,
                filter: "removed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_user_companies_company_id",
                table: "user_companies",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_company_permissions_expires_at",
                table: "user_company_permissions",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_company_permissions_membership_granted",
                table: "user_company_permissions",
                columns: new[] { "user_company_id", "is_granted" });

            migrationBuilder.CreateIndex(
                name: "IX_user_company_permissions_granted_by",
                table: "user_company_permissions",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_permissions_permission_id",
                table: "user_company_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_provinces_province_id",
                table: "user_company_provinces",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_roles_role_id",
                table: "user_company_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_company_warehouses_primary",
                table: "user_company_warehouses",
                columns: new[] { "user_company_id", "is_primary" },
                unique: true,
                filter: "is_primary = true");

            migrationBuilder.CreateIndex(
                name: "IX_user_company_warehouses_warehouse_id",
                table: "user_company_warehouses",
                column: "warehouse_id");

            // Expand/backfill invariant: every live legacy user receives one live membership.
            // created_by is intentionally left null because older deployments do not have the
            // optional users.created_by column. The membership audit field can be populated by
            // subsequent membership mutations.
            migrationBuilder.Sql("""
                INSERT INTO user_companies
                    (user_id, company_id, authorization_version, created_at)
                SELECT u."Id", u."CompanyId", 0, u.created_at
                FROM users AS u
                WHERE u.deleted_at IS NULL
                ON CONFLICT DO NOTHING;
                """);

            // Ordinary roles move to the membership path. SUPER_ADMIN remains in user_roles.
            migrationBuilder.Sql("""
                INSERT INTO user_company_roles
                    (user_company_id, role_id, expires_at, assigned_at)
                SELECT uc."Id", ur.role_id, ur.expires_at, ur.assigned_at
                FROM user_roles AS ur
                JOIN users AS u ON u."Id" = ur.user_id
                JOIN user_companies AS uc
                  ON uc.user_id = ur.user_id
                 AND uc.company_id = u."CompanyId"
                 AND uc.removed_at IS NULL
                JOIN roles AS r ON r."Id" = ur.role_id
                WHERE u.deleted_at IS NULL
                  AND r."Name" <> 'SUPER_ADMIN'
                  AND (r."CompanyId" IS NULL OR r."CompanyId" = u."CompanyId")
                ON CONFLICT DO NOTHING;
                """);

            // Legacy warehouse data must not cross companies during migration.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM user_warehouses AS uw
                        JOIN users AS u ON u."Id" = uw.user_id
                        JOIN warehouse_shadows AS w ON w."Id" = uw.warehouse_id
                        WHERE u."CompanyId" IS DISTINCT FROM w."CompanyId"
                    ) THEN
                        RAISE EXCEPTION 'User company membership backfill aborted: legacy warehouse assignment crosses company boundary';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_company_warehouses
                    (user_company_id, warehouse_id, is_primary)
                SELECT uc."Id", uw.warehouse_id, uw.is_primary
                FROM user_warehouses AS uw
                JOIN users AS u ON u."Id" = uw.user_id
                JOIN user_companies AS uc
                  ON uc.user_id = uw.user_id
                 AND uc.company_id = u."CompanyId"
                 AND uc.removed_at IS NULL
                JOIN warehouse_shadows AS w
                  ON w."Id" = uw.warehouse_id
                 AND w."CompanyId" = uc.company_id
                WHERE u.deleted_at IS NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_company_provinces (user_company_id, province_id)
                SELECT uc."Id", up.province_id
                FROM user_provinces AS up
                JOIN users AS u ON u."Id" = up.user_id
                JOIN user_companies AS uc
                  ON uc.user_id = up.user_id
                 AND uc.company_id = u."CompanyId"
                 AND uc.removed_at IS NULL
                WHERE u.deleted_at IS NULL
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.Sql("""
                INSERT INTO user_company_permissions
                    (user_company_id, permission_id, is_granted, granted_by, granted_at,
                     expires_at, reason, constraints)
                SELECT uc."Id", up.permission_id, up.is_granted, up.granted_by, up.granted_at,
                       up.expires_at, up.reason, up.constraints
                FROM user_permissions AS up
                JOIN users AS u ON u."Id" = up.user_id
                JOIN user_companies AS uc
                  ON uc.user_id = up.user_id
                 AND uc.company_id = u."CompanyId"
                 AND uc.removed_at IS NULL
                WHERE u.deleted_at IS NULL
                ON CONFLICT DO NOTHING;
                """);

            // Reconciliation invariants fail deployment rather than silently cutting over with
            // incomplete or cross-company membership data.
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    -- Reconciliation: every live legacy user has its initial membership.
                    IF EXISTS (
                        SELECT 1
                        FROM users AS u
                        LEFT JOIN user_companies AS uc
                          ON uc.user_id = u."Id"
                         AND uc.company_id = u."CompanyId"
                         AND uc.removed_at IS NULL
                        WHERE u.deleted_at IS NULL
                          AND uc."Id" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'User company membership reconciliation failed: live user is missing initial membership';
                    END IF;

                    -- Reconciliation: there is only one live membership per user/company pair.
                    IF EXISTS (
                        SELECT 1
                        FROM user_companies
                        WHERE removed_at IS NULL
                        GROUP BY user_id, company_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'User company membership reconciliation failed: duplicate live membership';
                    END IF;

                    -- Reconciliation: every eligible legacy role has a membership copy.
                    IF EXISTS (
                        SELECT 1
                        FROM user_roles AS ur
                        JOIN users AS u ON u."Id" = ur.user_id
                        JOIN roles AS r ON r."Id" = ur.role_id
                        JOIN user_companies AS uc
                          ON uc.user_id = ur.user_id
                         AND uc.company_id = u."CompanyId"
                         AND uc.removed_at IS NULL
                        LEFT JOIN user_company_roles AS ucr
                          ON ucr.user_company_id = uc."Id"
                         AND ucr.role_id = ur.role_id
                        WHERE u.deleted_at IS NULL
                          AND r."Name" <> 'SUPER_ADMIN'
                          AND (r."CompanyId" IS NULL OR r."CompanyId" = u."CompanyId")
                          AND ucr.user_company_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'User company membership reconciliation failed: eligible role assignment was not copied';
                    END IF;

                    -- Reconciliation: every legacy warehouse scope has a membership copy.
                    IF EXISTS (
                        SELECT 1
                        FROM user_warehouses AS uw
                        JOIN users AS u ON u."Id" = uw.user_id
                        JOIN user_companies AS uc
                          ON uc.user_id = uw.user_id
                         AND uc.company_id = u."CompanyId"
                         AND uc.removed_at IS NULL
                        LEFT JOIN user_company_warehouses AS ucw
                          ON ucw.user_company_id = uc."Id"
                         AND ucw.warehouse_id = uw.warehouse_id
                        WHERE u.deleted_at IS NULL
                          AND ucw.user_company_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'User company membership reconciliation failed: warehouse assignment was not copied';
                    END IF;

                    -- Reconciliation: every legacy province scope has a membership copy.
                    IF EXISTS (
                        SELECT 1
                        FROM user_provinces AS up
                        JOIN users AS u ON u."Id" = up.user_id
                        JOIN user_companies AS uc
                          ON uc.user_id = up.user_id
                         AND uc.company_id = u."CompanyId"
                         AND uc.removed_at IS NULL
                        LEFT JOIN user_company_provinces AS ucp
                          ON ucp.user_company_id = uc."Id"
                         AND ucp.province_id = up.province_id
                        WHERE u.deleted_at IS NULL
                          AND ucp.user_company_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'User company membership reconciliation failed: province assignment was not copied';
                    END IF;

                    -- Reconciliation: every legacy permission override has a membership copy.
                    IF EXISTS (
                        SELECT 1
                        FROM user_permissions AS up
                        JOIN users AS u ON u."Id" = up.user_id
                        JOIN user_companies AS uc
                          ON uc.user_id = up.user_id
                         AND uc.company_id = u."CompanyId"
                         AND uc.removed_at IS NULL
                        LEFT JOIN user_company_permissions AS ucp
                          ON ucp.user_company_id = uc."Id"
                         AND ucp.permission_id = up.permission_id
                        WHERE u.deleted_at IS NULL
                          AND ucp.user_company_id IS NULL
                    ) THEN
                        RAISE EXCEPTION 'User company membership reconciliation failed: permission override was not copied';
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_company_permissions");

            migrationBuilder.DropTable(
                name: "user_company_provinces");

            migrationBuilder.DropTable(
                name: "user_company_roles");

            migrationBuilder.DropTable(
                name: "user_company_warehouses");

            migrationBuilder.DropTable(
                name: "user_companies");
        }
    }
}
