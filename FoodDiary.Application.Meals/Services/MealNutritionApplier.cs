using FoodDiary.Results;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Meals.Services;

internal static class MealNutritionApplier {
    public static async Task<Result> ApplyAsync(
        Meal meal,
        UserId userId,
        IMealNutritionService mealNutritionService,
        MealNutritionInput input,
        CancellationToken cancellationToken) {
        if (!input.IsNutritionAutoCalculated) {
            return ApplyManualNutrition(meal, input);
        }

        Result<MealNutritionSummary> nutritionResult = await mealNutritionService.CalculateAsync(meal, userId, cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return nutritionResult;
        }

        meal.ApplyNutrition(new MealNutritionUpdate(
            nutritionResult.Value.Calories,
            nutritionResult.Value.Proteins,
            nutritionResult.Value.Fats,
            nutritionResult.Value.Carbs,
            nutritionResult.Value.Fiber,
            nutritionResult.Value.Alcohol,
            IsAutoCalculated: true));
        return Result.Success();
    }

    private static Result ApplyManualNutrition(Meal meal, MealNutritionInput input) {
        Result<ManualNutritionInput> manualNutritionResult = ManualNutritionValidator.Validate(
            input.ManualCalories,
            input.ManualProteins,
            input.ManualFats,
            input.ManualCarbs,
            input.ManualFiber,
            input.ManualAlcohol);
        if (manualNutritionResult.IsFailure) {
            return manualNutritionResult;
        }

        ManualNutritionInput manual = manualNutritionResult.Value;
        meal.ApplyNutrition(new MealNutritionUpdate(
            manual.Calories,
            manual.Proteins,
            manual.Fats,
            manual.Carbs,
            manual.Fiber,
            manual.Alcohol,
            IsAutoCalculated: false,
            ManualCalories: manual.Calories,
            ManualProteins: manual.Proteins,
            ManualFats: manual.Fats,
            ManualCarbs: manual.Carbs,
            ManualFiber: manual.Fiber,
            ManualAlcohol: manual.Alcohol));
        return Result.Success();
    }
}
