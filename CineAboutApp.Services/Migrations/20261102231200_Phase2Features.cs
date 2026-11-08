using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Features : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reminders_Movies_MovieId",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_Reminders_MovieId",
                table: "Reminders");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_UserId",
                table: "QuizSessions");

            migrationBuilder.RenameColumn(
                name: "MovieId",
                table: "Reminders",
                newName: "TmdbId");

            migrationBuilder.RenameIndex(
                name: "IX_Reminders_UserId_MovieId",
                table: "Reminders",
                newName: "IX_Reminders_UserId_TmdbId");

            migrationBuilder.AddColumn<int>(
                name: "MovieId",
                table: "Threads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Reports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Reports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterUrl",
                table: "Reminders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Reminders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "QuizSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "ResponseSeconds",
                table: "QuizSessionAnswers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            // Existing rows: sessions played before this migration were finished, and reminders
            // pointed at local movies (their old MovieId now lives in TmdbId).
            migrationBuilder.Sql("UPDATE QuizSessions SET IsCompleted = 1");
            migrationBuilder.Sql(@"
                UPDATE r
                SET r.Title = m.Title, r.PosterUrl = m.PosterUrl, r.TmdbId = 900000 + r.TmdbId
                FROM Reminders r
                INNER JOIN Movies m ON m.Id = r.TmdbId");

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserGenrePreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGenrePreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGenrePreferences_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserGenrePreferences_MyAppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "MyAppUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ThreadTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThreadId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThreadTags_Threads_ThreadId",
                        column: x => x.ThreadId,
                        principalTable: "Threads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Threads_MovieId",
                table: "Threads",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReviewedByUserId",
                table: "Reports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_UserId_PlayedAt",
                table: "QuizSessions",
                columns: new[] { "UserId", "PlayedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadTags_TagId",
                table: "ThreadTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadTags_ThreadId_TagId",
                table: "ThreadTags",
                columns: new[] { "ThreadId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGenrePreferences_GenreId",
                table: "UserGenrePreferences",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGenrePreferences_UserId_GenreId",
                table: "UserGenrePreferences",
                columns: new[] { "UserId", "GenreId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_MyAppUsers_ReviewedByUserId",
                table: "Reports",
                column: "ReviewedByUserId",
                principalTable: "MyAppUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Threads_Movies_MovieId",
                table: "Threads",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_MyAppUsers_ReviewedByUserId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Threads_Movies_MovieId",
                table: "Threads");

            migrationBuilder.DropTable(
                name: "ThreadTags");

            migrationBuilder.DropTable(
                name: "UserGenrePreferences");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_Threads_MovieId",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReviewedByUserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_QuizSessions_UserId_PlayedAt",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "MovieId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "PosterUrl",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Reminders");

            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "QuizSessions");

            migrationBuilder.DropColumn(
                name: "ResponseSeconds",
                table: "QuizSessionAnswers");

            migrationBuilder.RenameColumn(
                name: "TmdbId",
                table: "Reminders",
                newName: "MovieId");

            migrationBuilder.RenameIndex(
                name: "IX_Reminders_UserId_TmdbId",
                table: "Reminders",
                newName: "IX_Reminders_UserId_MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_MovieId",
                table: "Reminders",
                column: "MovieId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizSessions_UserId",
                table: "QuizSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reminders_Movies_MovieId",
                table: "Reminders",
                column: "MovieId",
                principalTable: "Movies",
                principalColumn: "Id");
        }
    }
}
