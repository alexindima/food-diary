using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddWeeklyGoals : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.CreateTable(
                name: "WeeklyGoals",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetDays = table.Column<int>(type: "integer", nullable: false),
                    ReminderEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    TimeZoneOffsetMinutes = table.Column<int>(type: "integer", nullable: true),
                    LastReminderLocalDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_WeeklyGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyGoals_ReminderEnabled_WeekStartUtc",
                table: "WeeklyGoals",
                columns: ["ReminderEnabled", "WeekStartUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyGoals_UserId_WeekStartUtc",
                table: "WeeklyGoals",
                columns: ["UserId", "WeekStartUtc"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "WeeklyGoals");
        }
    }
}
