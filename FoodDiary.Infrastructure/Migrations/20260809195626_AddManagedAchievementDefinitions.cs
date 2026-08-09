using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddManagedAchievementDefinitions : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.CreateTable(
            name: "AchievementDefinitions",
            columns: table => new {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Metric = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Threshold = table.Column<int>(type: "integer", nullable: false),
                TitleRu = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                TitleEn = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                DescriptionRu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                DescriptionEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                CreatedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ModifiedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table => table.PrimaryKey("PK_AchievementDefinitions", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AchievementDefinitions_IsActive_SortOrder",
            table: "AchievementDefinitions",
            columns: ["IsActive", "SortOrder"]);
        migrationBuilder.CreateIndex(
            name: "IX_AchievementDefinitions_Key",
            table: "AchievementDefinitions",
            column: "Key",
            unique: true);

        migrationBuilder.Sql("""
            INSERT INTO "AchievementDefinitions"
                ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc")
            VALUES
                ('10000000-0000-0000-0000-000000000003', 'streak_3', 'streak', 'LongestStreak', 3, 'Серия 3 дня', '3-day streak', 'Отличный шаг к устойчивой привычке.', 'A strong first step toward a lasting habit.', 'local_fire_department', 10, TRUE, 1, CURRENT_TIMESTAMP),
                ('10000000-0000-0000-0000-000000000007', 'streak_7', 'streak', 'LongestStreak', 7, 'Серия 7 дней', '7-day streak', 'Неделя регулярного ведения дневника.', 'A full week of consistent tracking.', 'local_fire_department', 20, TRUE, 1, CURRENT_TIMESTAMP),
                ('10000000-0000-0000-0000-000000000014', 'streak_14', 'streak', 'LongestStreak', 14, 'Серия 14 дней', '14-day streak', 'Две недели устойчивой привычки.', 'Two weeks of a lasting habit.', 'local_fire_department', 30, TRUE, 1, CURRENT_TIMESTAMP),
                ('10000000-0000-0000-0000-000000000030', 'streak_30', 'streak', 'LongestStreak', 30, 'Серия 30 дней', '30-day streak', 'Месяц последовательного прогресса.', 'A month of consistent progress.', 'local_fire_department', 40, TRUE, 1, CURRENT_TIMESTAMP),
                ('10000000-0000-0000-0000-000000000060', 'streak_60', 'streak', 'LongestStreak', 60, 'Серия 60 дней', '60-day streak', 'Привычка стала частью образа жизни.', 'The habit has become part of your lifestyle.', 'local_fire_department', 50, TRUE, 1, CURRENT_TIMESTAMP),
                ('10000000-0000-0000-0000-000000000100', 'streak_100', 'streak', 'LongestStreak', 100, 'Серия 100 дней', '100-day streak', 'Сто дней осознанного питания.', 'One hundred days of mindful nutrition.', 'local_fire_department', 60, TRUE, 1, CURRENT_TIMESTAMP),
                ('20000000-0000-0000-0000-000000000010', 'meals_10', 'meals', 'TotalMeals', 10, '10 приёмов', '10 meals', 'Первые десять записей в дневнике.', 'Your first ten diary entries.', 'restaurant', 70, TRUE, 1, CURRENT_TIMESTAMP),
                ('20000000-0000-0000-0000-000000000050', 'meals_50', 'meals', 'TotalMeals', 50, '50 приёмов', '50 meals', 'Регулярность набирает силу.', 'Consistency is gaining momentum.', 'restaurant', 80, TRUE, 1, CURRENT_TIMESTAMP),
                ('20000000-0000-0000-0000-000000000100', 'meals_100', 'meals', 'TotalMeals', 100, '100 приёмов', '100 meals', 'Сотня осознанных записей.', 'One hundred mindful entries.', 'restaurant', 90, TRUE, 1, CURRENT_TIMESTAMP),
                ('20000000-0000-0000-0000-000000000500', 'meals_500', 'meals', 'TotalMeals', 500, '500 приёмов', '500 meals', 'Настоящее мастерство ведения дневника.', 'True diary tracking mastery.', 'restaurant', 100, TRUE, 1, CURRENT_TIMESTAMP);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.DropTable(name: "AchievementDefinitions");
    }
}
