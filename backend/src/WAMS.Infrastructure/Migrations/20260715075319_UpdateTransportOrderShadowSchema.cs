using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransportOrderShadowSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_transport_order_shadows_company_docno_blno",
                table: "transport_order_shadows");

            migrationBuilder.DropColumn(
                name: "container_no",
                table: "transport_order_shadows");

            migrationBuilder.DropColumn(
                name: "doc_date",
                table: "transport_order_shadows");

            migrationBuilder.DropColumn(
                name: "seal_no",
                table: "transport_order_shadows");

            migrationBuilder.AddColumn<string>(
                name: "vehicle_type",
                table: "transport_order_shadows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_transport_order_shadows_company_docno_blno_vehicleno",
                table: "transport_order_shadows",
                columns: new[] { "company_id", "doc_no", "bl_no", "vehicle_no" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_transport_order_shadows_company_docno_blno_vehicleno",
                table: "transport_order_shadows");

            migrationBuilder.DropColumn(
                name: "vehicle_type",
                table: "transport_order_shadows");

            migrationBuilder.AddColumn<string>(
                name: "container_no",
                table: "transport_order_shadows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "doc_date",
                table: "transport_order_shadows",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "seal_no",
                table: "transport_order_shadows",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_transport_order_shadows_company_docno_blno",
                table: "transport_order_shadows",
                columns: new[] { "company_id", "doc_no", "bl_no" },
                unique: true);
        }
    }
}
