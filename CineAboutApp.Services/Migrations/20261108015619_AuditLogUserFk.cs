using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nothing is changed when a historical audit row points at a user that does not exist: the migration stops instead.
            migrationBuilder.Sql(@"IF EXISTS (SELECT 1 FROM AuditLogs a WHERE a.PerformedByUserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM MyAppUsers u WHERE u.Id = a.PerformedByUserId))
    THROW 51000, 'AuditLogs reference a user that does not exist; the foreign key was not created.', 1;");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_MyAppUsers_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId",
                principalTable: "MyAppUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_MyAppUsers_PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogs");
        }
    }
}
