using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineAboutApp.Services.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeedHistories",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedHistories", x => x.Key);
                });

            // Databases that existed before SeedHistory: if people already carry catalog photos (real http URLs), the
            // curated enrichment has demonstrably been applied, so it is marked as done and is not run again.
            // "Some person exists" is deliberately NOT used as proof; a half-seeded database has no such photos,
            // gets no marker and the (fill-only) enrichment runs once at the next start. The initial-creation step
            // is not marked here: it runs once at runtime, fills only EMPTY tables and then writes its own marker.
            migrationBuilder.Sql(@"
INSERT INTO [SeedHistories] ([Key], [AppliedAt], [Note])
SELECT 'Seed.CatalogEnrichment.v1', SYSUTCDATETIME(), 'Marked by migration: catalog photos were already present'
WHERE EXISTS (SELECT 1 FROM [Persons] WHERE [PhotoUrl] LIKE 'http%');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeedHistories");
        }
    }
}
