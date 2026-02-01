using System;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Recipes.Services;

public static class RecipeNutritionUpdater
{
    private const double Tolerance = 0.01;

    public static async Task EnsureNutritionAsync(
        Recipe recipe,
        IRecipeRepository repository,
        CancellationToken cancellationToken = default)
    {
        if (!recipe.IsNutritionAutoCalculated)
        {
            return;
        }

        var summary = RecipeNutritionCalculator.Calculate(recipe);
        if (!NeedsUpdate(recipe, summary))
        {
            return;
        }

        recipe.ApplyComputedNutrition(
            summary.TotalCalories,
            summary.TotalProteins,
            summary.TotalFats,
            summary.TotalCarbs,
            summary.TotalFiber,
            summary.TotalAlcohol);
        await repository.UpdateNutritionAsync(recipe, cancellationToken);
    }

    private static bool NeedsUpdate(Recipe recipe, RecipeNutritionSummary summary) =>
        !AreClose(recipe.TotalCalories, summary.TotalCalories)
        || !AreClose(recipe.TotalProteins, summary.TotalProteins)
        || !AreClose(recipe.TotalFats, summary.TotalFats)
        || !AreClose(recipe.TotalCarbs, summary.TotalCarbs)
        || !AreClose(recipe.TotalFiber, summary.TotalFiber)
        || !AreClose(recipe.TotalAlcohol, summary.TotalAlcohol);

    private static bool AreClose(double? left, double? right)
    {
        if (!left.HasValue && !right.HasValue)
        {
            return true;
        }

        if (!left.HasValue || !right.HasValue)
        {
            return false;
        }

        return Math.Abs(left.Value - right.Value) <= Tolerance;
    }
}
