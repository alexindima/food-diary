using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodDiary.Infrastructure.Migrations {
    /// <inheritdoc />
    public partial class ConvertMealSatietyToFivePointScale : Migration {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                UPDATE "Meals"
                SET
                    "PreMealSatietyLevel" = CASE
                        WHEN "PreMealSatietyLevel" = 0 THEN 3
                        WHEN "PreMealSatietyLevel" <= 2 THEN 1
                        WHEN "PreMealSatietyLevel" <= 4 THEN 2
                        WHEN "PreMealSatietyLevel" <= 6 THEN 3
                        WHEN "PreMealSatietyLevel" <= 8 THEN 4
                        ELSE 5
                    END,
                    "PostMealSatietyLevel" = CASE
                        WHEN "PostMealSatietyLevel" = 0 THEN 3
                        WHEN "PostMealSatietyLevel" <= 2 THEN 1
                        WHEN "PostMealSatietyLevel" <= 4 THEN 2
                        WHEN "PostMealSatietyLevel" <= 6 THEN 3
                        WHEN "PostMealSatietyLevel" <= 8 THEN 4
                        ELSE 5
                    END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                UPDATE "Meals"
                SET
                    "PreMealSatietyLevel" = CASE
                        WHEN "PreMealSatietyLevel" <= 1 THEN 2
                        WHEN "PreMealSatietyLevel" = 2 THEN 4
                        WHEN "PreMealSatietyLevel" = 3 THEN 6
                        WHEN "PreMealSatietyLevel" = 4 THEN 8
                        ELSE 9
                    END,
                    "PostMealSatietyLevel" = CASE
                        WHEN "PostMealSatietyLevel" <= 1 THEN 2
                        WHEN "PostMealSatietyLevel" = 2 THEN 4
                        WHEN "PostMealSatietyLevel" = 3 THEN 6
                        WHEN "PostMealSatietyLevel" = 4 THEN 8
                        ELSE 9
                    END
                """);
        }
    }
}
