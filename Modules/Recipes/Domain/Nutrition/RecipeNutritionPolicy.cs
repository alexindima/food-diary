namespace FoodDiary.Modules.Recipes.Domain.Nutrition;

public static class RecipeNutritionPolicy {
    public static RecipeNutritionValues Calculate(
        IEnumerable<RecipeNutritionIngredient> ingredients,
        RecipeNutritionValues stored, bool hasUncalculatedIngredients = false) {
        double calories = 0, proteins = 0, fats = 0, carbs = 0, fiber = 0, alcohol = 0;
        bool hasValues = false;
        foreach (RecipeNutritionIngredient ingredient in ingredients) {
            if (ingredient.Kind == RecipeNutritionSource.None) {
                continue;
            }
            RecipeNutritionValues? source = ingredient.Nutrition;
            double factor = ingredient.ScaleFactor;
            if (hasUncalculatedIngredients && source?.TotalCalories is null && source?.TotalProteins is null
                && source?.TotalFats is null && source?.TotalCarbs is null) {
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
        if (hasValues) {
            return new RecipeNutritionValues(Round(calories), Round(proteins), Round(fats), Round(carbs), Round(fiber), Round(alcohol));
        }
        return hasUncalculatedIngredients
            ? new RecipeNutritionValues(TotalCalories: null, TotalProteins: null, TotalFats: null,
                TotalCarbs: null, TotalFiber: null, TotalAlcohol: null)
            : stored;
    }

    public static RecipeNutritionValues SelectManual(RecipeNutritionValues manual, RecipeNutritionValues stored) =>
        new(manual.TotalCalories ?? stored.TotalCalories, manual.TotalProteins ?? stored.TotalProteins,
            manual.TotalFats ?? stored.TotalFats, manual.TotalCarbs ?? stored.TotalCarbs,
            manual.TotalFiber ?? stored.TotalFiber, manual.TotalAlcohol ?? stored.TotalAlcohol);

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.ToEven);
}
