using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Services;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class DiaryPdfGeneratorTests {
    [Fact]
    public void Generate_WithMeals_ReturnsPdfDocument() {
        var userId = UserId.New();
        var meals = new[] {
            CreateMeal(userId, new DateTime(2026, 5, 2, 21, 4, 0, DateTimeKind.Utc), 946, 59, 45, 76, 7),
            CreateMeal(userId, new DateTime(2026, 5, 3, 20, 41, 0, DateTimeKind.Utc), 905, 58, 45, 66, 5),
            CreateMeal(userId, new DateTime(2026, 5, 4, 15, 2, 0, DateTimeKind.Utc), 41, 1, 0, 10, 3),
        };
        var generator = new DiaryPdfGenerator();

        var pdf = generator.Generate(
            meals,
            new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 5, 5, 19, 59, 59, DateTimeKind.Utc));

        Assert.True(pdf.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    private static Meal CreateMeal(
        UserId userId,
        DateTime date,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber) {
        var meal = Meal.Create(userId, date, MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(calories, proteins, fats, carbs, fiber, 0, IsAutoCalculated: true));
        return meal;
    }
}
