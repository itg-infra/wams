using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogAddForensicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "audit_log",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "audit_log",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "audit_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_fullname",
                table: "audit_log",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_audit_log_company_created",
                table: "audit_log",
                columns: new[] { "company_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_audit_log_company_created",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "user_fullname",
                table: "audit_log");
        }
    }
}
