using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WidenTaxTypeUniqueIndexToIncludeCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tax_types_company_id_code",
                table: "tax_types");

            migrationBuilder.CreateIndex(
                name: "ix_tax_types_company_id_category_code",
                table: "tax_types",
                columns: new[] { "company_id", "category", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tax_types_company_id_category_code",
                table: "tax_types");

            migrationBuilder.CreateIndex(
                name: "ix_tax_types_company_id_code",
                table: "tax_types",
                columns: new[] { "company_id", "code" },
                unique: true);
        }
    }
}
