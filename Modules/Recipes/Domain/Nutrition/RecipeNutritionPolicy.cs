namespace FoodDiary.Modules.Recipes.Domain.Nutrition;

public static class RecipeNutritionPolicy {
    public static RecipeNutritionValues Calculate(
        IEnumerable<RecipeNutritionIngredient> ingredients,
        RecipeNutritionValues stored) {
        double calories = 0, proteins = 0, fats = 0, carbs = 0, fiber = 0, alcohol = 0;
        bool hasValues = false;
        foreach (RecipeNutritionIngredient ingredient in ingredients) {
            RecipeNutritionValues? source;
            double factor;
            if (ingredient.ProductBaseAmount is > 0) {
                source = ingredient.Product;
                factor = ingredient.Amount / ingredient.ProductBaseAmount.Value;
            } else if (ingredient.NestedRecipeServings is > 0) {
                source = ingredient.NestedRecipe;
                factor = ingredient.Amount / ingredient.NestedRecipeServings.Value;
            } else {
                continue;
            }
            calories += (source?.TotalCalories ?? 0) * factor;
            proteins += (source?.TotalProteins ?? 0) * factor;
            fats += (source?.TotalFats ?? 0) * factor;
            carbs += (source?.TotalCarbs ?? 0) * factor;
            fiber += (source?.TotalFiber ?? 0) * factor;
            alcohol += (source?.TotalAlcohol ?? 0) * factor;
            hasValues = true;
        }
        return hasValues
            ? new RecipeNutritionValues(Round(calories), Round(proteins), Round(fats), Round(carbs), Round(fiber), Round(alcohol))
            : stored;
    }

    public static RecipeNutritionValues SelectManual(RecipeNutritionValues manual, RecipeNutritionValues stored) =>
        new(manual.TotalCalories ?? stored.TotalCalories, manual.TotalProteins ?? stored.TotalProteins,
            manual.TotalFats ?? stored.TotalFats, manual.TotalCarbs ?? stored.TotalCarbs,
            manual.TotalFiber ?? stored.TotalFiber, manual.TotalAlcohol ?? stored.TotalAlcohol);

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
