using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <inheritdoc />
    public partial class BlacklistedTokenUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "BlacklistedTokens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlacklistedTokens_UserId",
                table: "BlacklistedTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BlacklistedTokens_MyAppUsers_UserId",
                table: "BlacklistedTokens",
                column: "UserId",
                principalTable: "MyAppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BlacklistedTokens_MyAppUsers_UserId",
                table: "BlacklistedTokens");

            migrationBuilder.DropIndex(
                name: "IX_BlacklistedTokens_UserId",
                table: "BlacklistedTokens");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BlacklistedTokens");
        }
    }
}
