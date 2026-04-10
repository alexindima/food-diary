using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFastingReminderThresholds : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.AddColumn<int>(
            name: "FastingCheckInFollowUpReminderHours",
            table: "Users",
            type: "integer",
            nullable: false,
            defaultValue: 20);

        migrationBuilder.AddColumn<int>(
            name: "FastingCheckInReminderHours",
            table: "Users",
            type: "integer",
            nullable: false,
            defaultValue: 12);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropColumn(
            name: "FastingCheckInFollowUpReminderHours",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "FastingCheckInReminderHours",
            table: "Users");
    }
}
