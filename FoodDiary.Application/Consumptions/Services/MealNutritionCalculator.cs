using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

public static class MealNutritionCalculator {
    public static MealNutritionSummary Calculate(
        Meal meal,
        IReadOnlyDictionary<ProductId, Product> products,
        IReadOnlyDictionary<RecipeId, Recipe> recipes) {
        double calories = 0;
        double proteins = 0;
        double fats = 0;
        double carbs = 0;
        double fiber = 0;
        double alcohol = 0;

        foreach (MealItem item in meal.Items) {
            MealNutritionSummary itemSummary = CalculateItem(item, products, recipes);
            calories += itemSummary.Calories;
            proteins += itemSummary.Proteins;
            fats += itemSummary.Fats;
            carbs += itemSummary.Carbs;
            fiber += itemSummary.Fiber;
            alcohol += itemSummary.Alcohol;
        }

        foreach (MealAiSession session in meal.AiSessions) {
            foreach (MealAiItem aiItem in session.Items) {
                if (aiItem.Resolution == MealAiItemResolution.Rejected) {
                    continue;
                }

                calories += aiItem.Calories;
                proteins += aiItem.Proteins;
                fats += aiItem.Fats;
                carbs += aiItem.Carbs;
                fiber += aiItem.Fiber;
                alcohol += aiItem.Alcohol;
            }
        }

        return new MealNutritionSummary(calories, proteins, fats, carbs, fiber, alcohol);
    }

    private static MealNutritionSummary CalculateItem(
        MealItem item,
        IReadOnlyDictionary<ProductId, Product> products,
        IReadOnlyDictionary<RecipeId, Recipe> recipes) {
        if (item.HasNutritionSnapshot) {
            double baseAmount = item.SnapshotBaseAmount!.Value <= 0 ? 1 : item.SnapshotBaseAmount.Value;
            double snapshotMultiplier = item.Amount / baseAmount;
            return new MealNutritionSummary(
                item.SnapshotCaloriesPerBase!.Value * snapshotMultiplier,
                item.SnapshotProteinsPerBase!.Value * snapshotMultiplier,
                item.SnapshotFatsPerBase!.Value * snapshotMultiplier,
                item.SnapshotCarbsPerBase!.Value * snapshotMultiplier,
                item.SnapshotFiberPerBase!.Value * snapshotMultiplier,
                item.SnapshotAlcoholPerBase!.Value * snapshotMultiplier);
        }

        if (item.ProductId is { } productId && products.TryGetValue(productId, out Product? product)) {
            double baseAmount = product.BaseAmount <= 0 ? 1 : product.BaseAmount;
            double productMultiplier = item.Amount / baseAmount;
            return new MealNutritionSummary(
                product.CaloriesPerBase * productMultiplier,
                product.ProteinsPerBase * productMultiplier,
                product.FatsPerBase * productMultiplier,
                product.CarbsPerBase * productMultiplier,
                product.FiberPerBase * productMultiplier,
                product.AlcoholPerBase * productMultiplier);
        }

        if (item.RecipeId is not { } recipeId || !recipes.TryGetValue(recipeId, out Recipe? recipe)) {
            return new MealNutritionSummary(0, 0, 0, 0, 0, 0);
        }

        int servings = recipe.Servings <= 0 ? 1 : recipe.Servings;
        double recipeMultiplier = item.Amount / servings;
        return new MealNutritionSummary(
            (recipe.TotalCalories ?? 0) * recipeMultiplier,
            (recipe.TotalProteins ?? 0) * recipeMultiplier,
            (recipe.TotalFats ?? 0) * recipeMultiplier,
            (recipe.TotalCarbs ?? 0) * recipeMultiplier,
            (recipe.TotalFiber ?? 0) * recipeMultiplier,
            (recipe.TotalAlcohol ?? 0) * recipeMultiplier);
    }
}
