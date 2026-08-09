using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public partial class AddAcademyAchievementDefinitions : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("""
            INSERT INTO "AchievementDefinitions"
                ("Id", "Key", "Category", "Metric", "Threshold", "TitleRu", "TitleEn", "DescriptionRu", "DescriptionEn", "Icon", "SortOrder", "IsActive", "Version", "CreatedOnUtc")
            VALUES
                ('30000000-0000-0000-0000-000000000001', 'academy_articles_1', 'academy', 'TotalAcademyArticlesRead', 1, 'Первая статья', 'First article', 'Первый шаг к более осознанному питанию.', 'Your first step toward more mindful nutrition.', 'school', 110, TRUE, 1, CURRENT_TIMESTAMP),
                ('30000000-0000-0000-0000-000000000005', 'academy_articles_5', 'academy', 'TotalAcademyArticlesRead', 5, 'Любознательный читатель', 'Curious reader', 'Пять статей Академии уже прочитано.', 'You have read five Academy articles.', 'school', 120, TRUE, 1, CURRENT_TIMESTAMP),
                ('30000000-0000-0000-0000-000000000010', 'academy_articles_10', 'academy', 'TotalAcademyArticlesRead', 10, 'Знаток питания', 'Nutrition scholar', 'Десять статей превращают знания в привычку.', 'Ten articles are turning knowledge into habit.', 'school', 130, TRUE, 1, CURRENT_TIMESTAMP),
                ('30000000-0000-0000-0000-000000000025', 'academy_articles_25', 'academy', 'TotalAcademyArticlesRead', 25, 'Выпускник Академии', 'Academy graduate', 'Серьёзная база знаний о питании собрана.', 'You have built a strong foundation of nutrition knowledge.', 'school', 140, TRUE, 1, CURRENT_TIMESTAMP);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) {
        migrationBuilder.Sql("""
            DELETE FROM "AchievementDefinitions"
            WHERE "Key" IN ('academy_articles_1', 'academy_articles_5', 'academy_articles_10', 'academy_articles_25');
            """);
    }
}
