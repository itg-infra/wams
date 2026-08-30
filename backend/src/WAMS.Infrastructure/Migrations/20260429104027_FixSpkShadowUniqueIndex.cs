using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSpkShadowUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_spk_shadows_company_doc_no",
                table: "spk_shadows");

            migrationBuilder.CreateIndex(
                name: "ix_spk_shadows_company_doc_no_item",
                table: "spk_shadows",
                columns: new[] { "company_id", "doc_no", "item_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_spk_shadows_company_doc_no_item",
                table: "spk_shadows");

            migrationBuilder.CreateIndex(
                name: "ix_spk_shadows_company_doc_no",
                table: "spk_shadows",
                columns: new[] { "company_id", "doc_no" },
                unique: true);
        }
    }
}
