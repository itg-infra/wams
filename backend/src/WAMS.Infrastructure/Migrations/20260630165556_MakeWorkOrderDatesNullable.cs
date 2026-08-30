using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeWorkOrderDatesNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            // Backfill: unscheduled work orders were created with DateTime.MinValue, which Npgsql
            // serialized to the Postgres special value '-infinity'. Now that the columns are nullable,
            // convert those placeholders to NULL so date filters and displays treat them as "unset".
            migrationBuilder.Sql(
                "UPDATE work_orders SET start_date = NULL WHERE start_date = '-infinity';");
            migrationBuilder.Sql(
                "UPDATE work_orders SET end_date = NULL WHERE end_date = '-infinity';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the '-infinity' placeholder for any NULLs before reinstating NOT NULL.
            migrationBuilder.Sql(
                "UPDATE work_orders SET start_date = '-infinity' WHERE start_date IS NULL;");
            migrationBuilder.Sql(
                "UPDATE work_orders SET end_date = '-infinity' WHERE end_date IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
