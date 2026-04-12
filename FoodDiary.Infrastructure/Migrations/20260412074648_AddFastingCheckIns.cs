using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFastingCheckIns : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "FastingCheckIns",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurrenceId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CheckedInAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                HungerLevel = table.Column<int>(type: "integer", nullable: false),
                EnergyLevel = table.Column<int>(type: "integer", nullable: false),
                MoodLevel = table.Column<int>(type: "integer", nullable: false),
                Symptoms = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_FastingCheckIns", x => x.Id);
                table.ForeignKey(
                    name: "FK_FastingCheckIns_FastingOccurrences_OccurrenceId",
                    column: x => x.OccurrenceId,
                    principalTable: "FastingOccurrences",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FastingCheckIns_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO "FastingCheckIns" (
                "Id",
                "OccurrenceId",
                "UserId",
                "CheckedInAtUtc",
                "HungerLevel",
                "EnergyLevel",
                "MoodLevel",
                "Symptoms",
                "Notes",
                "CreatedOnUtc",
                "ModifiedOnUtc"
            )
            SELECT
                "Id",
                "Id",
                "UserId",
                "CheckInAtUtc",
                COALESCE("HungerLevel", 3),
                COALESCE("EnergyLevel", 3),
                COALESCE("MoodLevel", 3),
                "Symptoms",
                "CheckInNotes",
                COALESCE("CheckInAtUtc", "CreatedOnUtc"),
                "ModifiedOnUtc"
            FROM "FastingOccurrences"
            WHERE "CheckInAtUtc" IS NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_FastingCheckIns_OccurrenceId_CheckedInAtUtc",
            table: "FastingCheckIns",
            columns: ["OccurrenceId", "CheckedInAtUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_FastingCheckIns_UserId_CheckedInAtUtc",
            table: "FastingCheckIns",
            columns: ["UserId", "CheckedInAtUtc"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "FastingCheckIns");
    }
}
