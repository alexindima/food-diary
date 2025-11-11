using System;
using System.Linq;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Recipes.Services;

public static class RecipeNutritionCalculator
{
    public static RecipeNutritionSummary Calculate(Recipe recipe)
    {
        if (!recipe.Steps.Any() || recipe.Steps.All(step => !step.Ingredients.Any()))
        {
            return FromStoredNutrition(recipe);
        }

        double totalCalories = 0;
        double totalProteins = 0;
        double totalFats = 0;
        double totalCarbs = 0;
        double totalFiber = 0;
        var hasComputedValues = false;

        foreach (var step in recipe.Steps)
        {
            foreach (var ingredient in step.Ingredients)
            {
                if (ingredient.Product is { } product && product.BaseAmount > 0)
                {
                    var factor = ingredient.Amount / product.BaseAmount;
                    totalCalories += product.CaloriesPerBase * factor;
                    totalProteins += product.ProteinsPerBase * factor;
                    totalFats += product.FatsPerBase * factor;
                    totalCarbs += product.CarbsPerBase * factor;
                    totalFiber += product.FiberPerBase * factor;
                    hasComputedValues = true;
                }
                else if (ingredient.NestedRecipe is { } nested && nested.Servings > 0)
                {
                    var factor = ingredient.Amount / nested.Servings;
                    totalCalories += (nested.TotalCalories ?? 0) * factor;
                    totalProteins += (nested.TotalProteins ?? 0) * factor;
                    totalFats += (nested.TotalFats ?? 0) * factor;
                    totalCarbs += (nested.TotalCarbs ?? 0) * factor;
                    hasComputedValues = true;
                }
            }
        }

        if (!hasComputedValues)
        {
            return FromStoredNutrition(recipe);
        }

        return new RecipeNutritionSummary(
            Math.Round(totalCalories, 2),
            Math.Round(totalProteins, 2),
            Math.Round(totalFats, 2),
            Math.Round(totalCarbs, 2),
            Math.Round(totalFiber, 2));
    }

    private static RecipeNutritionSummary FromStoredNutrition(Recipe recipe) =>
        new(
            recipe.TotalCalories,
            recipe.TotalProteins,
            recipe.TotalFats,
            recipe.TotalCarbs,
            null);
}

public sealed record RecipeNutritionSummary(
    double? TotalCalories,
    double? TotalProteins,
    double? TotalFats,
    double? TotalCarbs,
    double? TotalFiber);
