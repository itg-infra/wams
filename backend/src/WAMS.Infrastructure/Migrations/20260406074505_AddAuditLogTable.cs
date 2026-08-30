using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    company_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    table_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    record_id = table.Column<long>(type: "bigint", nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    request_path = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_company_id",
                table: "audit_log",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_created_at",
                table: "audit_log",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_record_id",
                table: "audit_log",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_table_name",
                table: "audit_log",
                column: "table_name");

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_user_id",
                table: "audit_log",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");
        }
    }
}
