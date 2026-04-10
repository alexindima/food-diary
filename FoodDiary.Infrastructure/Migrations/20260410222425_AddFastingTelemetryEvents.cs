using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFastingTelemetryEvents : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "FastingTelemetryEvents",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Protocol = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                PlanType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                OccurrenceKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                ReminderPresetId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                ReminderSource = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                FirstReminderHours = table.Column<int>(type: "integer", nullable: true),
                FollowUpReminderHours = table.Column<int>(type: "integer", nullable: true),
                PlannedDurationHours = table.Column<int>(type: "integer", nullable: true),
                ActualDurationHours = table.Column<double>(type: "double precision", nullable: true),
                HungerLevel = table.Column<int>(type: "integer", nullable: true),
                EnergyLevel = table.Column<int>(type: "integer", nullable: true),
                MoodLevel = table.Column<int>(type: "integer", nullable: true),
                SymptomsCount = table.Column<int>(type: "integer", nullable: true),
                HadNotes = table.Column<bool>(type: "boolean", nullable: true),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => {
                table.PrimaryKey("PK_FastingTelemetryEvents", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FastingTelemetryEvents_Name_OccurredAtUtc",
            table: "FastingTelemetryEvents",
            columns: new[] { "Name", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_FastingTelemetryEvents_OccurredAtUtc",
            table: "FastingTelemetryEvents",
            column: "OccurredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_FastingTelemetryEvents_ReminderPresetId_Name_OccurredAtUtc",
            table: "FastingTelemetryEvents",
            columns: new[] { "ReminderPresetId", "Name", "OccurredAtUtc" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(
            name: "FastingTelemetryEvents");
    }
}
