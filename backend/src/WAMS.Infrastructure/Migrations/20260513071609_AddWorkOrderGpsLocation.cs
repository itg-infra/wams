using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderGpsLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "gps_accuracy",
                table: "work_orders",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gps_latitude",
                table: "work_orders",
                type: "numeric(10,7)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gps_longitude",
                table: "work_orders",
                type: "numeric(11,7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "gps_recorded_at",
                table: "work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "chk_work_orders_gps_coherence",
                table: "work_orders",
                sql: "(gps_latitude IS NULL AND gps_longitude IS NULL AND gps_recorded_at IS NULL) OR (gps_latitude IS NOT NULL AND gps_longitude IS NOT NULL AND gps_recorded_at IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "chk_work_orders_gps_coherence",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "gps_accuracy",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "gps_latitude",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "gps_longitude",
                table: "work_orders");

            migrationBuilder.DropColumn(
                name: "gps_recorded_at",
                table: "work_orders");
        }
    }
}
