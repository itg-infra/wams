using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProvinceAndRegionScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "province_id",
                table: "warehouse_shadows",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "province_id",
                table: "budget_templates",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "provinces",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "province_aliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    province_id = table.Column<long>(type: "bigint", nullable: false),
                    alias = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_province_aliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_province_aliases_provinces_province_id",
                        column: x => x.province_id,
                        principalTable: "provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_provinces",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    province_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_provinces", x => new { x.user_id, x.province_id });
                    table.ForeignKey(
                        name: "FK_user_provinces_provinces_province_id",
                        column: x => x.province_id,
                        principalTable: "provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_provinces_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_warehouse_shadows_province_id",
                table: "warehouse_shadows",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "idx_budget_templates_province_id",
                table: "budget_templates",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "ix_province_aliases_alias",
                table: "province_aliases",
                column: "alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_province_aliases_province_id",
                table: "province_aliases",
                column: "province_id");

            migrationBuilder.CreateIndex(
                name: "ix_provinces_code",
                table: "provinces",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_provinces_name",
                table: "provinces",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_provinces_province_id",
                table: "user_provinces",
                column: "province_id");

            migrationBuilder.AddForeignKey(
                name: "FK_budget_templates_provinces_province_id",
                table: "budget_templates",
                column: "province_id",
                principalTable: "provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_warehouse_shadows_provinces_province_id",
                table: "warehouse_shadows",
                column: "province_id",
                principalTable: "provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Seed the canonical provinces + aliases (idempotent) so the backfill below
            // works even on an existing database where the app seeder has not yet run.
            // The runtime DatabaseSeeder is also idempotent and will skip these codes.
            migrationBuilder.Sql(@"
                INSERT INTO provinces (code, name, display, is_active, created_at)
                SELECT v.code, v.name, v.display, true, now()
                FROM (VALUES
                    ('GLOBAL','GLOBAL','Global'),
                    ('ID-JI','JAWA TIMUR','Jawa Timur'),
                    ('ID-JT','JAWA TENGAH','Jawa Tengah'),
                    ('ID-JK','JAKARTA','Jakarta'),
                    ('ID-SN','SULAWESI SELATAN','Sulawesi Selatan'),
                    ('ID-ST','SULAWESI TENGAH','Sulawesi Tengah'),
                    ('ID-SR','SULAWESI BARAT','Sulawesi Barat'),
                    ('ID-SU','SUMATERA UTARA','Sumatera Utara'),
                    ('ID-LA','LAMPUNG','Lampung'),
                    ('ID-JA','JAMBI','Jambi'),
                    ('ID-NB','NUSA TENGGARA BARAT','Nusa Tenggara Barat'),
                    ('ID-KI','KALIMANTAN TIMUR','Kalimantan Timur'),
                    ('ID-BB','BANGKA BELITUNG','Bangka Belitung')
                ) AS v(code, name, display)
                WHERE NOT EXISTS (SELECT 1 FROM provinces p WHERE p.code = v.code);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO province_aliases (province_id, alias, created_at)
                SELECT p.""Id"", a.alias, now()
                FROM (VALUES
                    ('ID-JK','DKI JAKARTA'),
                    ('ID-BB','KEPULAUAN BANGKA BELITUNG')
                ) AS a(code, alias)
                JOIN provinces p ON p.code = a.code
                WHERE NOT EXISTS (SELECT 1 FROM province_aliases pa WHERE pa.alias = a.alias);
            ");

            // Backfill budget_templates.province_id from the old free-text location,
            // normalized the same way ProvinceNormalizer does (trim, collapse whitespace, upper),
            // matching against province name first, then alias.
            migrationBuilder.Sql(@"
                UPDATE budget_templates bt
                SET province_id = p.""Id""
                FROM provinces p
                WHERE bt.location IS NOT NULL
                  AND UPPER(REGEXP_REPLACE(TRIM(bt.location), '\s+', ' ', 'g')) = p.name;
            ");

            migrationBuilder.Sql(@"
                UPDATE budget_templates bt
                SET province_id = pa.province_id
                FROM province_aliases pa
                WHERE bt.province_id IS NULL
                  AND bt.location IS NOT NULL
                  AND UPPER(REGEXP_REPLACE(TRIM(bt.location), '\s+', ' ', 'g')) = pa.alias;
            ");

            // Finally drop the now-migrated free-text column.
            migrationBuilder.DropColumn(
                name: "location",
                table: "budget_templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_budget_templates_provinces_province_id",
                table: "budget_templates");

            migrationBuilder.DropForeignKey(
                name: "FK_warehouse_shadows_provinces_province_id",
                table: "warehouse_shadows");

            migrationBuilder.DropTable(
                name: "province_aliases");

            migrationBuilder.DropTable(
                name: "user_provinces");

            migrationBuilder.DropTable(
                name: "provinces");

            migrationBuilder.DropIndex(
                name: "idx_warehouse_shadows_province_id",
                table: "warehouse_shadows");

            migrationBuilder.DropIndex(
                name: "idx_budget_templates_province_id",
                table: "budget_templates");

            migrationBuilder.DropColumn(
                name: "province_id",
                table: "warehouse_shadows");

            migrationBuilder.DropColumn(
                name: "province_id",
                table: "budget_templates");

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "budget_templates",
                type: "text",
                nullable: true);
        }
    }
}
