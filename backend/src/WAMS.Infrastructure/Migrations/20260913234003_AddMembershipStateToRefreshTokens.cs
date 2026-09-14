using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipStateToRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "membership_authorization_version",
                table: "refresh_tokens",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "user_company_id",
                table: "refresh_tokens",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_refresh_tokens_user_company_revoked",
                table: "refresh_tokens",
                columns: new[] { "user_company_id", "revoked_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_user_companies_user_company_id",
                table: "refresh_tokens",
                column: "user_company_id",
                principalTable: "user_companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_user_companies_user_company_id",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "idx_refresh_tokens_user_company_revoked",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "membership_authorization_version",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "user_company_id",
                table: "refresh_tokens");
        }
    }
}
