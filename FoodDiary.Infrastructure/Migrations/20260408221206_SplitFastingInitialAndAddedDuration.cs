using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SplitFastingInitialAndAddedDuration : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            name: "AddedDurationHours",
            table: "FastingSessions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "InitialPlannedDurationHours",
            table: "FastingSessions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE "FastingSessions"
            SET "InitialPlannedDurationHours" = "PlannedDurationHours",
                "AddedDurationHours" = 0
            """);

        migrationBuilder.DropColumn(
            name: "PlannedDurationHours",
            table: "FastingSessions");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            name: "PlannedDurationHours",
            table: "FastingSessions",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE "FastingSessions"
            SET "PlannedDurationHours" = "InitialPlannedDurationHours" + "AddedDurationHours"
            """);

        migrationBuilder.DropColumn(
            name: "AddedDurationHours",
            table: "FastingSessions");

        migrationBuilder.DropColumn(
            name: "InitialPlannedDurationHours",
            table: "FastingSessions");
    }
}
