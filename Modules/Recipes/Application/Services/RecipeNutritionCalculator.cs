using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Nutrition;

namespace FoodDiary.Modules.Recipes.Application.Services;

public static class RecipeNutritionCalculator {
    public static RecipeNutritionSummary Calculate(Recipe recipe) {
        int missing = recipe.Steps.SelectMany(step => step.Ingredients)
            .Sum(ingredient => ingredient.TextName is not null ? 1 : ingredient.NestedRecipe?.MissingIngredientCount ?? 0);
        RecipeNutritionValues values = RecipeNutritionPolicy.Calculate(
            recipe.Steps.SelectMany(step => step.Ingredients).Select(ingredient => new RecipeNutritionIngredient(
                ingredient.Amount,
                ingredient.ProductSnapshot?.BaseAmount,
                ingredient.ProductSnapshot is { } product
                    ? new RecipeNutritionValues(product.CaloriesPerBase, product.ProteinsPerBase, product.FatsPerBase,
                        product.CarbsPerBase, product.FiberPerBase, product.AlcoholPerBase) : null,
                ingredient.NestedRecipe?.Servings,
                ingredient.NestedRecipe is { } nested
                    ? new RecipeNutritionValues(nested.TotalCalories, nested.TotalProteins, nested.TotalFats,
                        nested.TotalCarbs, nested.TotalFiber, nested.TotalAlcohol) : null)),
            new RecipeNutritionValues(recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats,
                recipe.TotalCarbs, recipe.TotalFiber, recipe.TotalAlcohol), missing > 0);
        return new(values.TotalCalories, values.TotalProteins, values.TotalFats,
            values.TotalCarbs, values.TotalFiber, values.TotalAlcohol) { MissingIngredientCount = missing };
    }
}
