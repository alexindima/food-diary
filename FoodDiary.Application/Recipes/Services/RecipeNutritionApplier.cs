using FoodDiary.Results;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Services;

internal static class RecipeNutritionApplier {
    public static Result Apply(
        Recipe recipe,
        bool calculateNutritionAutomatically,
        double? manualCalories,
        double? manualProteins,
        double? manualFats,
        double? manualCarbs,
        double? manualFiber,
        double? manualAlcohol) {
        if (calculateNutritionAutomatically) {
            recipe.EnableAutoNutrition();
            return Result.Success();
        }

        Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> manualNutritionResult = RecipeManualNutritionValidator.Validate(
            manualCalories,
            manualProteins,
            manualFats,
            manualCarbs,
            manualFiber,
            manualAlcohol);
        if (manualNutritionResult.IsFailure) {
            return manualNutritionResult;
        }

        (double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol) = manualNutritionResult.Value;
        recipe.SetManualNutrition(
            Calories,
            Proteins,
            Fats,
            Carbs,
            Fiber,
            Alcohol);
        return Result.Success();
    }
}
