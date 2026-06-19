using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Nutrition;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Consumptions.Services;

public static class ManualNutritionValidator {
    private const string CaloriesField = "ManualCalories";
    private const string ProteinsField = "ManualProteins";
    private const string FatsField = "ManualFats";
    private const string CarbsField = "ManualCarbs";
    private const string FiberField = "ManualFiber";
    private const string AlcoholField = "ManualAlcohol";

    public static Result<ManualNutritionInput> Validate(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        if (!calories.HasValue) {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(CaloriesField));
        }

        if (!proteins.HasValue) {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(ProteinsField));
        }

        if (!fats.HasValue) {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(FatsField));
        }

        if (!carbs.HasValue) {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(CarbsField));
        }

        if (!fiber.HasValue) {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(FiberField));
        }

        if (calories < 0 || proteins < 0 || fats < 0 || carbs < 0 || fiber < 0 || alcohol < 0) {
            return Result.Failure<ManualNutritionInput>(
                Errors.Validation.Invalid("ManualNutrition", "Values must be greater than or equal to 0."));
        }

        if (calories > ManualNutritionLimits.MaxCalories) {
            return Result.Failure<ManualNutritionInput>(
                Errors.Validation.Invalid(CaloriesField, ManualNutritionLimits.MaxCaloriesErrorMessage));
        }

        foreach ((double? value, string field) in GetNutrientValues(proteins, fats, carbs, fiber, alcohol)) {
            if (value > ManualNutritionLimits.MaxNutrient) {
                return Result.Failure<ManualNutritionInput>(
                    Errors.Validation.Invalid(field, ManualNutritionLimits.MaxNutrientErrorMessage));
            }
        }

        return Result.Success(new ManualNutritionInput(
            calories.Value,
            proteins.Value,
            fats.Value,
            carbs.Value,
            fiber.Value,
            alcohol ?? 0));
    }

    private static IEnumerable<(double? Value, string Field)> GetNutrientValues(
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol) {
        yield return (proteins, ProteinsField);
        yield return (fats, FatsField);
        yield return (carbs, CarbsField);
        yield return (fiber, FiberField);
        yield return (alcohol, AlcoholField);
    }
}
