using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRateCardCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rate_cards_vendor_shadow_id",
                table: "rate_cards");

            migrationBuilder.CreateIndex(
                name: "idx_rate_cards_vendor_status_submitted",
                table: "rate_cards",
                columns: new[] { "vendor_shadow_id", "status", "submitted_at" },
                descending: new[] { false, false, true },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_rate_cards_vendor_status_submitted",
                table: "rate_cards");

            migrationBuilder.CreateIndex(
                name: "IX_rate_cards_vendor_shadow_id",
                table: "rate_cards",
                column: "vendor_shadow_id");
        }
    }
}
