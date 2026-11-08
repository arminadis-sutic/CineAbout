using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <summary>
    /// City and quiz category become reference tables (Upute 3.1: FK instead of strings).
    /// Existing text is converted, not dropped: every distinct city (per country of the user) and every distinct category
    /// becomes a reference row and the profiles / questions are pointed at it. If any text could not be mapped the migration
    /// stops with THROW before the old columns are removed (the migration is not recorded and nothing is lost).
    /// </summary>
    public partial class CityAndQuizCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuizCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(name: "IX_Cities_CountryId", table: "Cities", column: "CountryId");
            migrationBuilder.CreateIndex(name: "IX_Cities_Name_CountryId", table: "Cities", columns: new[] { "Name", "CountryId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_QuizCategories_Name", table: "QuizCategories", column: "Name", unique: true);

            // Texts that do not fit the reference columns stop the migration before anything is changed.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [UserProfiles] WHERE LEN(LTRIM(RTRIM([City]))) > 200)
    THROW 51004, 'Migration stopped: a City text is longer than 200 characters; shorten it first.', 1;
IF EXISTS (SELECT 1 FROM [QuizQuestions] WHERE LEN(LTRIM(RTRIM([Category]))) > 200)
    THROW 51005, 'Migration stopped: a quiz Category text is longer than 200 characters; shorten it first.', 1;");

            migrationBuilder.AddColumn<int>(name: "CityId", table: "UserProfiles", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "CategoryId", table: "QuizQuestions", type: "int", nullable: true);

            // Categories: one row per distinct text (case-insensitive); an empty text becomes 'Ostalo'.
            migrationBuilder.Sql(@"
INSERT INTO [QuizCategories] ([Name])
SELECT DISTINCT CASE WHEN LTRIM(RTRIM([Category])) = N'' THEN N'Ostalo' ELSE LTRIM(RTRIM([Category])) END
FROM [QuizQuestions];");
            migrationBuilder.Sql(@"
UPDATE q SET q.[CategoryId] = c.[Id]
FROM [QuizQuestions] q
INNER JOIN [QuizCategories] c ON c.[Name] = CASE WHEN LTRIM(RTRIM(q.[Category])) = N'' THEN N'Ostalo' ELSE LTRIM(RTRIM(q.[Category])) END;");

            // Cities: one row per distinct text and country of the user who wrote it (the same text in two countries is two cities).
            migrationBuilder.Sql(@"
INSERT INTO [Cities] ([Name], [CountryId])
SELECT DISTINCT LTRIM(RTRIM(p.[City])), u.[CountryId]
FROM [UserProfiles] p
INNER JOIN [MyAppUsers] u ON u.[Id] = p.[UserId]
WHERE p.[City] IS NOT NULL AND LTRIM(RTRIM(p.[City])) <> N'';");
            migrationBuilder.Sql(@"
UPDATE p SET p.[CityId] = c.[Id]
FROM [UserProfiles] p
INNER JOIN [MyAppUsers] u ON u.[Id] = p.[UserId]
INNER JOIN [Cities] c ON c.[Name] = LTRIM(RTRIM(p.[City])) AND c.[CountryId] = u.[CountryId]
WHERE p.[City] IS NOT NULL AND LTRIM(RTRIM(p.[City])) <> N'';");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [QuizQuestions] WHERE [CategoryId] IS NULL)
    THROW 51006, 'Migration stopped: a quiz question could not be mapped to a category; no column was removed.', 1;
IF EXISTS (SELECT 1 FROM [UserProfiles] WHERE [City] IS NOT NULL AND LTRIM(RTRIM([City])) <> N'' AND [CityId] IS NULL)
    THROW 51007, 'Migration stopped: a profile city could not be mapped; no column was removed.', 1;");

            migrationBuilder.AlterColumn<int>(name: "CategoryId", table: "QuizQuestions", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.DropColumn(name: "City", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "Category", table: "QuizQuestions");

            migrationBuilder.CreateIndex(name: "IX_UserProfiles_CityId", table: "UserProfiles", column: "CityId");
            migrationBuilder.CreateIndex(name: "IX_QuizQuestions_CategoryId", table: "QuizQuestions", column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizQuestions_QuizCategories_CategoryId",
                table: "QuizQuestions",
                column: "CategoryId",
                principalTable: "QuizCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Cities_CityId",
                table: "UserProfiles",
                column: "CityId",
                principalTable: "Cities",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "City", table: "UserProfiles", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Category", table: "QuizQuestions", type: "nvarchar(max)", nullable: false, defaultValue: "");

            migrationBuilder.Sql(@"UPDATE p SET p.[City] = c.[Name] FROM [UserProfiles] p INNER JOIN [Cities] c ON c.[Id] = p.[CityId];");
            migrationBuilder.Sql(@"UPDATE q SET q.[Category] = c.[Name] FROM [QuizQuestions] q INNER JOIN [QuizCategories] c ON c.[Id] = q.[CategoryId];");

            migrationBuilder.DropForeignKey(name: "FK_QuizQuestions_QuizCategories_CategoryId", table: "QuizQuestions");
            migrationBuilder.DropForeignKey(name: "FK_UserProfiles_Cities_CityId", table: "UserProfiles");
            migrationBuilder.DropTable(name: "Cities");
            migrationBuilder.DropTable(name: "QuizCategories");
            migrationBuilder.DropIndex(name: "IX_UserProfiles_CityId", table: "UserProfiles");
            migrationBuilder.DropIndex(name: "IX_QuizQuestions_CategoryId", table: "QuizQuestions");
            migrationBuilder.DropColumn(name: "CityId", table: "UserProfiles");
            migrationBuilder.DropColumn(name: "CategoryId", table: "QuizQuestions");
        }
    }
}
