using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Tests.Nutrition;

[ExcludeFromCodeCoverage]
public sealed class NutritionMappingCompatibilityTests {
    [Theory]
    [InlineData(0, 0, 50, "yellow")]
    [InlineData(100, 0.1, 26, "red")]
    [InlineData(100, 0.3, 26, "red")]
    public void RecipesMappings_PreserveSharedQuality(double calories, double fiber, int expectedScore, string expectedGrade) {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Quality sample", 1);
        recipe.SetManualNutrition(calories, 0, 0, 0, fiber, 0);

        FoodDiary.Modules.Recipes.Application.Models.RecipeModel recipeModel = recipe.ToModel(0, isOwnedByCurrentUser: true);

        Assert.Multiple(
            () => Assert.Equal(expectedScore, recipeModel.QualityScore),
            () => Assert.Equal(expectedGrade, recipeModel.QualityGrade));
    }
}
