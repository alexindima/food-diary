using FoodDiary.Modules.Meals.Application.Mappings;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.ValueObjects;

namespace FoodDiary.Modules.Meals.Application.Tests.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class NutritionMappingCompatibilityTests {
    [Theory]
    [InlineData(0, 0, 50, "yellow")]
    [InlineData(100, 0.1, 26, "red")]
    [InlineData(100, 0.3, 26, "red")]
    public void MealsMappings_PreserveSharedQuality(double calories, double fiber, int expectedScore, string expectedGrade) {
        var userId = UserId.New();
        var meal = Meal.Create(userId, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));
        meal.ApplyNutrition(new MealNutritionUpdate(calories, 0, 0, 0, fiber, 0, IsAutoCalculated: true));

        FoodDiary.Modules.Meals.Service.Contracts.Models.MealModel mealModel = meal.ToModel();

        Assert.Multiple(
            () => Assert.Equal(expectedScore, mealModel.QualityScore),
            () => Assert.Equal(expectedGrade, mealModel.QualityGrade));
    }
}
