using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AddUserGoalHistory : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<long>(
                name: "GoalRevision",
                table: "UserNutritionProfiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "UserGoalHistory",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DailyCalorieTarget = table.Column<double>(type: "double precision", nullable: true),
                    ProteinTarget = table.Column<double>(type: "double precision", nullable: true),
                    FatTarget = table.Column<double>(type: "double precision", nullable: true),
                    CarbTarget = table.Column<double>(type: "double precision", nullable: true),
                    FiberTarget = table.Column<double>(type: "double precision", nullable: true),
                    WaterGoal = table.Column<double>(type: "double precision", nullable: true),
                    HydrationGoal = table.Column<double>(type: "double precision", nullable: true),
                    StepGoal = table.Column<int>(type: "integer", nullable: true),
                    CalorieCyclingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MondayCalories = table.Column<double>(type: "double precision", nullable: true),
                    TuesdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    WednesdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    ThursdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    FridayCalories = table.Column<double>(type: "double precision", nullable: true),
                    SaturdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    SundayCalories = table.Column<double>(type: "double precision", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserGoalHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGoalHistory_UserNutritionProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "UserNutritionProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGoalHistory_UserId_EffectiveFrom_Revision",
                table: "UserGoalHistory",
                columns: ["UserId", "EffectiveFrom", "Revision"]);

            migrationBuilder.CreateIndex(
                name: "IX_UserGoalHistory_UserId_Revision",
                table: "UserGoalHistory",
                columns: ["UserId", "Revision"]);

            // Historical values erased before this release cannot be reconstructed.
            // Preserve actual current values (including nulls) as the initial baseline.
            migrationBuilder.Sql("""
                INSERT INTO "UserGoalHistory" (
                    "Id", "UserId", "Revision", "EffectiveFrom", "RecordedAtUtc", "DailyCalorieTarget",
                    "ProteinTarget", "FatTarget", "CarbTarget", "FiberTarget", "WaterGoal", "HydrationGoal", "StepGoal",
                    "CalorieCyclingEnabled", "MondayCalories", "TuesdayCalories", "WednesdayCalories", "ThursdayCalories",
                    "FridayCalories", "SaturdayCalories", "SundayCalories")
                SELECT gen_random_uuid(), "Id", 0, DATE '-infinity', CURRENT_TIMESTAMP, "DailyCalorieTarget",
                    "ProteinTarget", "FatTarget", "CarbTarget", "FiberTarget", "WaterGoal", "HydrationGoal", "StepGoal",
                    "CalorieCyclingEnabled", "MondayCalories", "TuesdayCalories", "WednesdayCalories", "ThursdayCalories",
                    "FridayCalories", "SaturdayCalories", "SundayCalories"
                FROM "UserNutritionProfiles";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.DropTable(
                name: "UserGoalHistory");

            migrationBuilder.DropColumn(
                name: "GoalRevision",
                table: "UserNutritionProfiles");
        }
    }
}
