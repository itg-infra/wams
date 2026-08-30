using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBpReminderCooldownIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_type_reference_created_at",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_type_recipient_created_at",
                table: "notifications",
                columns: new[] { "type", "recipient_user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notifications_type_recipient_created_at",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_type_reference_created_at",
                table: "notifications",
                columns: new[] { "type", "reference_id", "created_at" });
        }
    }
}
