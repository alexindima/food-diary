using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Application.Common;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Meals.Application.Services;

internal static class MealNutritionApplier {
    public static async Task<Result> ApplyAsync(
        Meal meal,
        UserId userId,
        IMealNutritionService mealNutritionService,
        MealNutritionInput input,
        CancellationToken cancellationToken) {
        ManualNutritionInput? manual = null;
        if (!input.IsNutritionAutoCalculated) {
            Result<ManualNutritionInput> manualValidation = ManualNutritionValidator.Validate(
                input.ManualCalories, input.ManualProteins, input.ManualFats,
                input.ManualCarbs, input.ManualFiber, input.ManualAlcohol);
            if (manualValidation.IsFailure) {
                return manualValidation;
            }
            manual = manualValidation.Value;
        }
        Result<MealNutritionSummary> nutritionResult = await mealNutritionService.CalculateAsync(meal, userId, cancellationToken).ConfigureAwait(false);
        if (nutritionResult.IsFailure) {
            return nutritionResult;
        }

        if (manual is not null) {
            return ApplyManualNutrition(meal, manual);
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

    private static Result ApplyManualNutrition(Meal meal, ManualNutritionInput manual) {
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
