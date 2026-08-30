using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WidenFumigationDosagePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "sulphur_fluoride_dosage",
                table: "work_order_fumigation_details",
                type: "numeric(11,4)",
                precision: 11,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,4)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "phosphine_dosage",
                table: "work_order_fumigation_details",
                type: "numeric(11,4)",
                precision: 11,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,4)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "methyl_bromide_dosage",
                table: "work_order_fumigation_details",
                type: "numeric(11,4)",
                precision: 11,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,4)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "sulphur_fluoride_dosage",
                table: "work_order_fumigation_details",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,4)",
                oldPrecision: 11,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "phosphine_dosage",
                table: "work_order_fumigation_details",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,4)",
                oldPrecision: 11,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "methyl_bromide_dosage",
                table: "work_order_fumigation_details",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(11,4)",
                oldPrecision: 11,
                oldScale: 4,
                oldNullable: true);
        }
    }
}
