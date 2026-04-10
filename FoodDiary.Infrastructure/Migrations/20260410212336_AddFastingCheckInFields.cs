using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

public partial class AddFastingCheckInFields : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<DateTime>(
            name: "CheckInAtUtc",
            table: "FastingOccurrences",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CheckInNotes",
            table: "FastingOccurrences",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EnergyLevel",
            table: "FastingOccurrences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "HungerLevel",
            table: "FastingOccurrences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "MoodLevel",
            table: "FastingOccurrences",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Symptoms",
            table: "FastingOccurrences",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            name: "CheckInAtUtc",
            table: "FastingOccurrences");

        migrationBuilder.DropColumn(
            name: "CheckInNotes",
            table: "FastingOccurrences");

        migrationBuilder.DropColumn(
            name: "EnergyLevel",
            table: "FastingOccurrences");

        migrationBuilder.DropColumn(
            name: "HungerLevel",
            table: "FastingOccurrences");

        migrationBuilder.DropColumn(
            name: "MoodLevel",
            table: "FastingOccurrences");

        migrationBuilder.DropColumn(
            name: "Symptoms",
            table: "FastingOccurrences");
    }
}
