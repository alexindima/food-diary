using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Services;

public static class RecipeNutritionCalculator {
    public static RecipeNutritionSummary Calculate(Recipe recipe) {
        if (!recipe.Steps.Any() || recipe.Steps.All(step => !step.Ingredients.Any())) {
            return FromStoredNutrition(recipe);
        }

        double totalCalories = 0;
        double totalProteins = 0;
        double totalFats = 0;
        double totalCarbs = 0;
        double totalFiber = 0;
        double totalAlcohol = 0;
        bool hasComputedValues = false;

        foreach (RecipeStep step in recipe.Steps) {
            foreach (RecipeIngredient ingredient in step.Ingredients) {
                if (ingredient.Product is { } product && product.BaseAmount > 0) {
                    double factor = ingredient.Amount / product.BaseAmount;
                    totalCalories += product.CaloriesPerBase * factor;
                    totalProteins += product.ProteinsPerBase * factor;
                    totalFats += product.FatsPerBase * factor;
                    totalCarbs += product.CarbsPerBase * factor;
                    totalFiber += product.FiberPerBase * factor;
                    totalAlcohol += product.AlcoholPerBase * factor;
                    hasComputedValues = true;
                } else if (ingredient.NestedRecipe is { } nested && nested.Servings > 0) {
                    double factor = ingredient.Amount / nested.Servings;
                    totalCalories += (nested.TotalCalories ?? 0) * factor;
                    totalProteins += (nested.TotalProteins ?? 0) * factor;
                    totalFats += (nested.TotalFats ?? 0) * factor;
                    totalCarbs += (nested.TotalCarbs ?? 0) * factor;
                    totalFiber += (nested.TotalFiber ?? 0) * factor;
                    totalAlcohol += (nested.TotalAlcohol ?? 0) * factor;
                    hasComputedValues = true;
                }
            }
        }

        if (!hasComputedValues) {
            return FromStoredNutrition(recipe);
        }

        return new RecipeNutritionSummary(
            Math.Round(totalCalories, 2, MidpointRounding.ToEven),
            Math.Round(totalProteins, 2, MidpointRounding.ToEven),
            Math.Round(totalFats, 2, MidpointRounding.ToEven),
            Math.Round(totalCarbs, 2, MidpointRounding.ToEven),
            Math.Round(totalFiber, 2, MidpointRounding.ToEven),
            Math.Round(totalAlcohol, 2, MidpointRounding.ToEven));
    }

    private static RecipeNutritionSummary FromStoredNutrition(Recipe recipe) =>
        new(
            recipe.TotalCalories,
            recipe.TotalProteins,
            recipe.TotalFats,
            recipe.TotalCarbs,
            recipe.TotalFiber,
            recipe.TotalAlcohol);
}
