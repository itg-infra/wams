using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogRemoveUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "audit_log");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "audit_log",
                newName: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "audit_log",
                newName: "Id");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "audit_log",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
