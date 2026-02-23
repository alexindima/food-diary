using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public class RecipeNutritionCalculatorTests
{
    [Fact]
    public void Calculate_WhenRecipeHasNoSteps_ReturnsStoredNutrition()
    {
        var recipe = Recipe.Create(
            UserId.New(),
            name: "Recipe",
            servings: 1);

        recipe.SetManualNutrition(
            calories: 320,
            proteins: 20,
            fats: 10,
            carbs: 30,
            fiber: 4,
            alcohol: 0);

        var result = RecipeNutritionCalculator.Calculate(recipe);

        Assert.Equal(320, result.TotalCalories);
        Assert.Equal(20, result.TotalProteins);
        Assert.Equal(10, result.TotalFats);
        Assert.Equal(30, result.TotalCarbs);
        Assert.Equal(4, result.TotalFiber);
        Assert.Equal(0, result.TotalAlcohol);
    }

    [Fact]
    public void Calculate_WhenStepIngredientsAreNotHydrated_ReturnsStoredNutrition()
    {
        var recipe = Recipe.Create(
            UserId.New(),
            name: "Recipe",
            servings: 1);

        recipe.ApplyComputedNutrition(
            calories: 111,
            proteins: 11,
            fats: 22,
            carbs: 33,
            fiber: 4,
            alcohol: 0);

        var step = recipe.AddStep(1, "Mix");
        step.AddProductIngredient(ProductId.New(), amount: 100);

        var result = RecipeNutritionCalculator.Calculate(recipe);

        Assert.Equal(111, result.TotalCalories);
        Assert.Equal(11, result.TotalProteins);
        Assert.Equal(22, result.TotalFats);
        Assert.Equal(33, result.TotalCarbs);
        Assert.Equal(4, result.TotalFiber);
        Assert.Equal(0, result.TotalAlcohol);
    }
}

