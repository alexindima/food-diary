using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Application.Mappings;
using FoodDiary.Modules.Meals.Domain.ValueObjects;

using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class NutritionConsumerCompatibilityTests {
    [Theory]
    [InlineData(0, 0, 50, "yellow")]
    [InlineData(100, 0.1, 26, "red")]
    [InlineData(100, 0.3, 26, "red")]
    public void ProductMealAndRecipeMappings_PreserveSharedQuality(double calories, double fiber, int expectedScore, string expectedGrade) {
        var userId = UserId.New();
        var product = Product.Create(userId, "Quality sample", MeasurementUnit.G, 100, 100, calories, 0, 0, 0, fiber, 0);
        var meal = Meal.Create(userId, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));
        meal.ApplyNutrition(new MealNutritionUpdate(calories, 0, 0, 0, fiber, 0, IsAutoCalculated: true));
        var recipe = Recipe.Create(userId, "Quality sample", 1);
        recipe.SetManualNutrition(calories, 0, 0, 0, fiber, 0);

        FoodDiary.Modules.Products.Application.Models.ProductModel productModel = product.ToModel();
        FoodDiary.Modules.Meals.Service.Contracts.Models.MealModel mealModel = meal.ToModel();
        FoodDiary.Modules.Recipes.Application.Models.RecipeModel recipeModel = recipe.ToModel(0, isOwnedByCurrentUser: true);

        Assert.Multiple(
            () => Assert.Equal(expectedScore, product.GetQualityScore().Score),
            () => Assert.Equal(expectedScore, productModel.QualityScore),
            () => Assert.Equal(expectedGrade, productModel.QualityGrade),
            () => Assert.Equal(expectedScore, mealModel.QualityScore),
            () => Assert.Equal(expectedGrade, mealModel.QualityGrade),
            () => Assert.Equal(expectedScore, recipeModel.QualityScore),
            () => Assert.Equal(expectedGrade, recipeModel.QualityGrade));
    }
}
