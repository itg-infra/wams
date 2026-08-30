using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendTaxTypeForSapSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tax_types_code",
                table: "tax_types");

            migrationBuilder.AddColumn<long>(
                name: "company_id",
                table: "tax_types",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "first_seen_at",
                table: "tax_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "synced_at",
                table: "tax_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "company_id", "first_seen_at", "is_active", "synced_at" },
                values: new object[] { 1L, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "company_id", "first_seen_at", "is_active", "synced_at" },
                values: new object[] { 1L, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "company_id", "first_seen_at", "is_active", "synced_at" },
                values: new object[] { 1L, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "company_id", "first_seen_at", "is_active", "synced_at" },
                values: new object[] { 1L, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "ix_tax_types_company_id_code",
                table: "tax_types",
                columns: new[] { "company_id", "code" },
                unique: true);

            migrationBuilder.Sql(
                "UPDATE tax_types SET company_id = (SELECT \"Id\" FROM companies ORDER BY \"Id\" LIMIT 1) " +
                "WHERE EXISTS (SELECT 1 FROM companies);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tax_types_company_id_code",
                table: "tax_types");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "tax_types");

            migrationBuilder.DropColumn(
                name: "first_seen_at",
                table: "tax_types");

            migrationBuilder.DropColumn(
                name: "synced_at",
                table: "tax_types");

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 1L,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 2L,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 3L,
                column: "is_active",
                value: true);

            migrationBuilder.UpdateData(
                table: "tax_types",
                keyColumn: "Id",
                keyValue: 4L,
                column: "is_active",
                value: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_types_code",
                table: "tax_types",
                column: "code",
                unique: true);
        }
    }
}
