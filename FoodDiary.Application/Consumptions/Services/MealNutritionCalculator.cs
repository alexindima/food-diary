using System.Collections.Generic;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Services;

public static class MealNutritionCalculator
{
    public static MealNutritionSummary Calculate(
        Meal meal,
        IReadOnlyDictionary<ProductId, Product> products,
        IReadOnlyDictionary<RecipeId, Recipe> recipes)
    {
        double calories = 0;
        double proteins = 0;
        double fats = 0;
        double carbs = 0;
        double fiber = 0;
        double alcohol = 0;

        foreach (var item in meal.Items)
        {
            if (item.ProductId is { } productId && products.TryGetValue(productId, out var product))
            {
                var baseAmount = product.BaseAmount <= 0 ? 1 : product.BaseAmount;
                var multiplier = item.Amount / baseAmount;
                calories += product.CaloriesPerBase * multiplier;
                proteins += product.ProteinsPerBase * multiplier;
                fats += product.FatsPerBase * multiplier;
                carbs += product.CarbsPerBase * multiplier;
                fiber += product.FiberPerBase * multiplier;
                alcohol += product.AlcoholPerBase * multiplier;
                continue;
            }

            if (item.RecipeId is { } recipeId && recipes.TryGetValue(recipeId, out var recipe) && recipe.Servings > 0)
            {
                var servings = recipe.Servings <= 0 ? 1 : recipe.Servings;
                var servingsMultiplier = item.Amount;
                calories += ((recipe.TotalCalories ?? 0) / servings) * servingsMultiplier;
                proteins += ((recipe.TotalProteins ?? 0) / servings) * servingsMultiplier;
                fats += ((recipe.TotalFats ?? 0) / servings) * servingsMultiplier;
                carbs += ((recipe.TotalCarbs ?? 0) / servings) * servingsMultiplier;
                fiber += ((recipe.TotalFiber ?? 0) / servings) * servingsMultiplier;
                alcohol += ((recipe.TotalAlcohol ?? 0) / servings) * servingsMultiplier;
            }
        }

        return new MealNutritionSummary(calories, proteins, fats, carbs, fiber, alcohol);
    }
}

public sealed record MealNutritionSummary(
    double Calories,
    double Proteins,
    double Fats,
    double Carbs,
    double Fiber,
    double Alcohol);
