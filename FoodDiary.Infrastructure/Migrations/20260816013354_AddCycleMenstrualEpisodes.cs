using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddCycleMenstrualEpisodes : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "CycleMenstrualEpisodes",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExcludedFromPredictions = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_CycleMenstrualEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleMenstrualEpisodes_CycleProfiles_CycleProfileId",
                        column: x => x.CycleProfileId,
                        principalTable: "CycleProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CycleMenstrualEpisodes_CycleProfileId_StartDate",
                table: "CycleMenstrualEpisodes",
                columns: ["CycleProfileId", "StartDate"],
                unique: true);

            migrationBuilder.Sql(
                """
                WITH bleeding_dates AS (
                    SELECT DISTINCT "CycleProfileId", "Date"
                    FROM "CycleBleedingEntries"
                    WHERE "Type" = 'Bleeding'
                ), marked AS (
                    SELECT
                        "CycleProfileId",
                        "Date",
                        CASE
                            WHEN LAG("Date") OVER (PARTITION BY "CycleProfileId" ORDER BY "Date") IS NULL THEN 1
                            WHEN "Date" - LAG("Date") OVER (PARTITION BY "CycleProfileId" ORDER BY "Date") > 2 THEN 1
                            ELSE 0
                        END AS new_episode
                    FROM bleeding_dates
                ), grouped AS (
                    SELECT
                        "CycleProfileId",
                        "Date",
                        SUM(new_episode) OVER (PARTITION BY "CycleProfileId" ORDER BY "Date") AS episode_number
                    FROM marked
                ), episodes AS (
                    SELECT "CycleProfileId", MIN("Date") AS start_date, MAX("Date") AS end_date
                    FROM grouped
                    GROUP BY "CycleProfileId", episode_number
                )
                INSERT INTO "CycleMenstrualEpisodes"
                    ("Id", "CycleProfileId", "StartDate", "EndDate", "Status", "ExcludedFromPredictions", "CreatedOnUtc")
                SELECT
                    MD5("CycleProfileId"::text || ':' || start_date::text)::uuid,
                    "CycleProfileId",
                    start_date,
                    end_date,
                    'Inferred',
                    FALSE,
                    NOW()
                FROM episodes;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "CycleMenstrualEpisodes");
        }
    }
}
