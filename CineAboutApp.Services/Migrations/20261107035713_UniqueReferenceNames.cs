using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <inheritdoc />
    public partial class UniqueReferenceNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Pre-check (F-102). The migration NEVER merges or deletes rows by itself: if the data already contains
            // duplicate names it stops here with a clear message, before anything is altered, and the duplicates have
            // to be resolved by hand following the per-table plan (audit-report/F-102-duplicate-plan.md).
            // The checks compare names the way the unique index will (the database collation, trailing spaces ignored).
            foreach (var table in new[] { "Genres", "Languages", "Countries", "AwardTypes" })
            {
                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM [{table}] GROUP BY [Name] HAVING COUNT(*) > 1)
    THROW 51002, 'Migration stopped: duplicate names exist in {table}. Resolve them first (see the F-102 duplicate plan); no data was changed.', 1;");
            }

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Studios] GROUP BY [Name], [CountryId] HAVING COUNT(*) > 1)
    THROW 51002, 'Migration stopped: duplicate (Name, CountryId) pairs exist in Studios. Resolve them first (see the F-102 duplicate plan); no data was changed.', 1;");

            foreach (var table in new[] { "Genres", "Languages", "Countries", "AwardTypes", "Studios" })
            {
                migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM [{table}] WHERE LEN([Name]) > 200)
    THROW 51003, 'Migration stopped: names longer than 200 characters exist in {table}; shorten them first.', 1;");
            }

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Studios",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Languages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Genres",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Countries",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AwardTypes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Studios_Name_CountryId",
                table: "Studios",
                columns: new[] { "Name", "CountryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Name",
                table: "Languages",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AwardTypes_Name",
                table: "AwardTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Studios_Name_CountryId",
                table: "Studios");

            migrationBuilder.DropIndex(
                name: "IX_Languages_Name",
                table: "Languages");

            migrationBuilder.DropIndex(
                name: "IX_Genres_Name",
                table: "Genres");

            migrationBuilder.DropIndex(
                name: "IX_Countries_Name",
                table: "Countries");

            migrationBuilder.DropIndex(
                name: "IX_AwardTypes_Name",
                table: "AwardTypes");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Studios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Languages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Genres",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Countries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AwardTypes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
