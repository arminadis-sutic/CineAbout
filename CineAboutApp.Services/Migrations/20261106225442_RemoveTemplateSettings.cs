using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTemplateSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoNotifyTeamsAboutSelection",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "MaxAllowedRequestsToJoinPerTeam",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "OnlyNotifyTeamOwner",
                table: "AppSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoNotifyTeamsAboutSelection",
                table: "AppSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxAllowedRequestsToJoinPerTeam",
                table: "AppSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "OnlyNotifyTeamOwner",
                table: "AppSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
