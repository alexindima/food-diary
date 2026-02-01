using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Consumptions.Services;

public static class ManualNutritionValidator
{
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
        double? alcohol)
    {
        if (!calories.HasValue)
        {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(CaloriesField));
        }

        if (!proteins.HasValue)
        {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(ProteinsField));
        }

        if (!fats.HasValue)
        {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(FatsField));
        }

        if (!carbs.HasValue)
        {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(CarbsField));
        }

        if (!fiber.HasValue)
        {
            return Result.Failure<ManualNutritionInput>(Errors.Validation.Required(FiberField));
        }

        if (calories < 0 || proteins < 0 || fats < 0 || carbs < 0 || fiber < 0 || alcohol < 0)
        {
            return Result.Failure<ManualNutritionInput>(
                Errors.Validation.Invalid("ManualNutrition", "Values must be greater than or equal to 0."));
        }

        return Result.Success(new ManualNutritionInput(
            calories.Value,
            proteins.Value,
            fats.Value,
            carbs.Value,
            fiber.Value,
            alcohol ?? 0));
    }
}
