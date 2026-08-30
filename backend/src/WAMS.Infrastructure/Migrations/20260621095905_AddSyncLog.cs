using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CompanyCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Added = table.Column<int>(type: "integer", nullable: false),
                    Updated = table.Column<int>(type: "integer", nullable: false),
                    Deactivated = table.Column<int>(type: "integer", nullable: false),
                    AbortReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sync_logs_Outcome_ServiceName_FinishedAt",
                table: "sync_logs",
                columns: new[] { "Outcome", "ServiceName", "FinishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_logs_ServiceName_StartedAt",
                table: "sync_logs",
                columns: new[] { "ServiceName", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_logs");
        }
    }
}
