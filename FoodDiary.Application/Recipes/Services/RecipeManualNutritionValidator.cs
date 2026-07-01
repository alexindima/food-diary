using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Nutrition;

namespace FoodDiary.Application.Recipes.Services;

internal static class RecipeManualNutritionValidator {
    public static Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> Validate(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        if (calories is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(calories)));
        }

        if (proteins is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(proteins)));
        }

        if (fats is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(fats)));
        }

        if (carbs is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(carbs)));
        }

        if (fiber is null) {
            return Result.Failure<(double, double, double, double, double, double)>(Errors.Validation.Required(nameof(fiber)));
        }

        if (calories < 0 || proteins < 0 || fats < 0 || carbs < 0 || fiber < 0 || alcohol < 0) {
            return Result.Failure<(double, double, double, double, double, double)>(
                Errors.Validation.Invalid("ManualNutrition", "Manual nutrition values must be greater than or equal to 0."));
        }

        if (calories > ManualNutritionLimits.MaxCalories) {
            return Result.Failure<(double, double, double, double, double, double)>(
                Errors.Validation.Invalid(nameof(calories), ManualNutritionLimits.MaxCaloriesErrorMessage));
        }

        if (proteins > ManualNutritionLimits.MaxNutrient ||
            fats > ManualNutritionLimits.MaxNutrient ||
            carbs > ManualNutritionLimits.MaxNutrient ||
            fiber > ManualNutritionLimits.MaxNutrient ||
            alcohol > ManualNutritionLimits.MaxNutrient) {
            return Result.Failure<(double, double, double, double, double, double)>(
                Errors.Validation.Invalid("ManualNutrition", ManualNutritionLimits.MaxNutrientErrorMessage));
        }

        return Result.Success((calories.Value, proteins.Value, fats.Value, carbs.Value, fiber.Value, alcohol ?? 0));
    }
}
