using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class SplitUserPreferencesAndNutritionProfile : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {

            migrationBuilder.CreateTable(
                name: "UserNutritionProfiles",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    WeightKg = table.Column<double>(type: "double precision", nullable: true),
                    DesiredWeightKg = table.Column<double>(type: "double precision", nullable: true),
                    DesiredWaistCm = table.Column<double>(type: "double precision", nullable: true),
                    HeightCm = table.Column<double>(type: "double precision", nullable: true),
                    ActivityLevel = table.Column<string>(type: "text", nullable: false),
                    DailyCalorieTarget = table.Column<double>(type: "double precision", nullable: true),
                    ProteinTarget = table.Column<double>(type: "double precision", nullable: true),
                    FatTarget = table.Column<double>(type: "double precision", nullable: true),
                    CarbTarget = table.Column<double>(type: "double precision", nullable: true),
                    FiberTarget = table.Column<double>(type: "double precision", nullable: true),
                    StepGoal = table.Column<int>(type: "integer", nullable: true),
                    WaterGoal = table.Column<double>(type: "double precision", nullable: true),
                    HydrationGoal = table.Column<double>(type: "double precision", nullable: true),
                    CalorieCyclingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MondayCalories = table.Column<double>(type: "double precision", nullable: true),
                    TuesdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    WednesdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    ThursdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    FridayCalories = table.Column<double>(type: "double precision", nullable: true),
                    SaturdayCalories = table.Column<double>(type: "double precision", nullable: true),
                    SundayCalories = table.Column<double>(type: "double precision", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserNutritionProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNutritionProfiles_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardLayout = table.Column<string>(type: "jsonb", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true, defaultValue: "en"),
                    Theme = table.Column<string>(type: "text", nullable: true, defaultValue: "ocean"),
                    UiStyle = table.Column<string>(type: "text", nullable: true, defaultValue: "classic"),
                    SurfaceStyle = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "normal"),
                    PushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FastingPushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SocialPushNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FastingCheckInReminderHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 12),
                    FastingCheckInFollowUpReminderHours = table.Column<int>(type: "integer", nullable: false, defaultValue: 20),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            // Copy every legacy value before removing its original column.
            migrationBuilder.Sql("""
                INSERT INTO "UserPreferences" ("Id", "CreatedOnUtc", "ModifiedOnUtc", "DashboardLayout", "Language", "Theme", "UiStyle", "SurfaceStyle", "PushNotificationsEnabled", "FastingPushNotificationsEnabled", "SocialPushNotificationsEnabled", "FastingCheckInReminderHours", "FastingCheckInFollowUpReminderHours", "TimeZoneId") SELECT "Id", "CreatedOnUtc", "ModifiedOnUtc", "DashboardLayout", "Language", "Theme", "UiStyle", "SurfaceStyle", "PushNotificationsEnabled", "FastingPushNotificationsEnabled", "SocialPushNotificationsEnabled", "FastingCheckInReminderHours", "FastingCheckInFollowUpReminderHours", "TimeZoneId" FROM "Users";
                INSERT INTO "UserNutritionProfiles" ("Id", "CreatedOnUtc", "ModifiedOnUtc", "BirthDate", "Gender", "WeightKg", "DesiredWeightKg", "DesiredWaistCm", "HeightCm", "ActivityLevel", "DailyCalorieTarget", "ProteinTarget", "FatTarget", "CarbTarget", "FiberTarget", "StepGoal", "WaterGoal", "HydrationGoal", "CalorieCyclingEnabled", "MondayCalories", "TuesdayCalories", "WednesdayCalories", "ThursdayCalories", "FridayCalories", "SaturdayCalories", "SundayCalories") SELECT "Id", "CreatedOnUtc", "ModifiedOnUtc", "BirthDate", "Gender", "WeightKg", "DesiredWeightKg", "DesiredWaistCm", "HeightCm", "ActivityLevel", "DailyCalorieTarget", "ProteinTarget", "FatTarget", "CarbTarget", "FiberTarget", "StepGoal", "WaterGoal", "HydrationGoal", "CalorieCyclingEnabled", "MondayCalories", "TuesdayCalories", "WednesdayCalories", "ThursdayCalories", "FridayCalories", "SaturdayCalories", "SundayCalories" FROM "Users";
                """);

            migrationBuilder.DropColumn(
    name: "BirthDate",
    table: "Users");

            migrationBuilder.DropColumn(
                name: "CarbTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DashboardLayout",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DesiredWeightKg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FastingCheckInReminderHours",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FatTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FridayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProteinTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SaturdayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StepGoal",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SurfaceStyle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ThursdayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TuesdayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WaterGoal",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActivityLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CalorieCyclingEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DailyCalorieTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DesiredWaistCm",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FastingCheckInFollowUpReminderHours",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FastingPushNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FiberTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "HydrationGoal",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MondayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PushNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SocialPushNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SundayCalories",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UiStyle",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WednesdayCalories",
                table: "Users");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {

            migrationBuilder.AddColumn<string>(
                name: "ActivityLevel",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CalorieCyclingEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "CarbTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DailyCalorieTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DashboardLayout",
                table: "Users",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DesiredWaistCm",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DesiredWeightKg",
                table: "Users",
                type: "double precision",
                nullable: true);

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

            migrationBuilder.AddColumn<bool>(
                name: "FastingPushNotificationsEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<double>(
                name: "FatTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FiberTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FridayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HeightCm",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "HydrationGoal",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Users",
                type: "text",
                nullable: true,
                defaultValue: "en");

            migrationBuilder.AddColumn<double>(
                name: "MondayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ProteinTarget",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PushNotificationsEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "SaturdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SocialPushNotificationsEnabled",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "StepGoal",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SundayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SurfaceStyle",
                table: "Users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "normal");

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Users",
                type: "text",
                nullable: true,
                defaultValue: "ocean");

            migrationBuilder.AddColumn<double>(
                name: "ThursdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TuesdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UiStyle",
                table: "Users",
                type: "text",
                nullable: true,
                defaultValue: "classic");

            migrationBuilder.AddColumn<double>(
                name: "WaterGoal",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WednesdayCalories",
                table: "Users",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WeightKg",
                table: "Users",
                type: "double precision",
                nullable: true);
            // Restore the latest independent values before dropping their tables.
            migrationBuilder.Sql("""
                UPDATE "Users" AS account SET "DashboardLayout" = state."DashboardLayout", "Language" = state."Language", "Theme" = state."Theme", "UiStyle" = state."UiStyle", "SurfaceStyle" = state."SurfaceStyle", "PushNotificationsEnabled" = state."PushNotificationsEnabled", "FastingPushNotificationsEnabled" = state."FastingPushNotificationsEnabled", "SocialPushNotificationsEnabled" = state."SocialPushNotificationsEnabled", "FastingCheckInReminderHours" = state."FastingCheckInReminderHours", "FastingCheckInFollowUpReminderHours" = state."FastingCheckInFollowUpReminderHours", "TimeZoneId" = state."TimeZoneId" FROM "UserPreferences" AS state WHERE account."Id" = state."Id";
                UPDATE "Users" AS account SET "BirthDate" = state."BirthDate", "Gender" = state."Gender", "WeightKg" = state."WeightKg", "DesiredWeightKg" = state."DesiredWeightKg", "DesiredWaistCm" = state."DesiredWaistCm", "HeightCm" = state."HeightCm", "ActivityLevel" = state."ActivityLevel", "DailyCalorieTarget" = state."DailyCalorieTarget", "ProteinTarget" = state."ProteinTarget", "FatTarget" = state."FatTarget", "CarbTarget" = state."CarbTarget", "FiberTarget" = state."FiberTarget", "StepGoal" = state."StepGoal", "WaterGoal" = state."WaterGoal", "HydrationGoal" = state."HydrationGoal", "CalorieCyclingEnabled" = state."CalorieCyclingEnabled", "MondayCalories" = state."MondayCalories", "TuesdayCalories" = state."TuesdayCalories", "WednesdayCalories" = state."WednesdayCalories", "ThursdayCalories" = state."ThursdayCalories", "FridayCalories" = state."FridayCalories", "SaturdayCalories" = state."SaturdayCalories", "SundayCalories" = state."SundayCalories" FROM "UserNutritionProfiles" AS state WHERE account."Id" = state."Id";
                ALTER TABLE "Users" ALTER COLUMN "ActivityLevel" DROP DEFAULT;
                """);

            migrationBuilder.DropTable(
    name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserNutritionProfiles");

        }
    }
}
