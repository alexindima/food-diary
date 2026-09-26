using FoodDiary.Modules.Recipes.Application.Services;
using FoodDiary.Modules.Recipes.Application.Mappings;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeTextNutritionTests {
    [Fact]
    public void Calculate_OnlyText_DoesNotReuseOldTotals() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        recipe.ApplyComputedNutrition(100, 10, 5, 20, 1, 0);
        recipe.AddStep(1, "Season").AddTextIngredient("Salt", "to taste");
        RecipeNutritionSummary result = RecipeNutritionCalculator.Calculate(recipe);
        Assert.Multiple(
            () => Assert.Equal(1, result.MissingIngredientCount),
            () => Assert.Null(result.TotalCalories),
            () => Assert.Null(result.TotalProteins),
            () => Assert.Null(result.TotalFats),
            () => Assert.Null(result.TotalCarbs),
            () => Assert.Null(result.TotalFiber),
            () => Assert.Null(result.TotalAlcohol));
    }

    [Fact]
    public void Mapping_PreservesTextAndManualNutritionResetsMissingCount() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        recipe.AddStep(1, "Season").AddTextIngredient("Salt", "to taste");
        recipe.SetMissingIngredientCount(1);
        recipe.SetManualNutrition(100, 10, 5, 20, 1, 0);
        RecipeModel model = recipe.ToModel(0, isOwnedByCurrentUser: true);
        RecipeIngredientModel ingredient = Assert.Single(Assert.Single(model.Steps).Ingredients);
        Assert.Multiple(
            () => Assert.Equal("Salt", ingredient.TextName),
            () => Assert.Equal("to taste", ingredient.AmountText),
            () => Assert.Equal(0, model.MissingIngredientCount),
            () => Assert.Equal(100, model.TotalCalories));
    }
}
