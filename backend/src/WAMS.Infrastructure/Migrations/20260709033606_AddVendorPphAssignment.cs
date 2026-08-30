using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorPphAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vendor_pph_assignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    vendor_shadow_id = table.Column<long>(type: "bigint", nullable: false),
                    tax_type_id = table.Column<long>(type: "bigint", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendor_pph_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vendor_pph_assignments_tax_types_tax_type_id",
                        column: x => x.tax_type_id,
                        principalTable: "tax_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vendor_pph_assignments_vendor_shadows_vendor_shadow_id",
                        column: x => x.vendor_shadow_id,
                        principalTable: "vendor_shadows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vendor_pph_assignments_tax_type_id",
                table: "vendor_pph_assignments",
                column: "tax_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_vendor_pph_assignments_vendor_tax_type",
                table: "vendor_pph_assignments",
                columns: new[] { "vendor_shadow_id", "tax_type_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vendor_pph_assignments");
        }
    }
}
