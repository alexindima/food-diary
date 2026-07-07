using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

internal static class ProductUpdateLimitValidator {
    public static Result Validate(Product product, UpdateProductCommand command, ProductUpdateValues values) {
        MeasurementUnit unit = values.Unit ?? product.BaseUnit;

        Result defaultPortionResult = ValidateMax(
            nameof(command.DefaultPortionAmount),
            command.DefaultPortionAmount ?? product.DefaultPortionAmount,
            Product.GetMaxDefaultPortionAmount(unit),
            "DefaultPortionAmount exceeds the maximum for the selected unit");
        if (defaultPortionResult.IsFailure) {
            return defaultPortionResult;
        }

        Result caloriesResult = ValidateMax(
            nameof(command.CaloriesPerBase),
            command.CaloriesPerBase ?? product.CaloriesPerBase,
            Product.GetMaxCaloriesPerBase(unit),
            "CaloriesPerBase exceeds the maximum for the selected unit");
        if (caloriesResult.IsFailure) {
            return caloriesResult;
        }

        double maxNutrient = Product.GetMaxNutrientPerBase(unit);
        return ValidateNutrients(product, command, maxNutrient);
    }

    private static Result ValidateNutrients(Product product, UpdateProductCommand command, double maxNutrient) {
        Result proteinsResult = ValidateMax(
            nameof(command.ProteinsPerBase),
            command.ProteinsPerBase ?? product.ProteinsPerBase,
            maxNutrient,
            "ProteinsPerBase exceeds the maximum for the selected unit");
        if (proteinsResult.IsFailure) {
            return proteinsResult;
        }

        Result fatsResult = ValidateMax(
            nameof(command.FatsPerBase),
            command.FatsPerBase ?? product.FatsPerBase,
            maxNutrient,
            "FatsPerBase exceeds the maximum for the selected unit");
        if (fatsResult.IsFailure) {
            return fatsResult;
        }

        Result carbsResult = ValidateMax(
            nameof(command.CarbsPerBase),
            command.CarbsPerBase ?? product.CarbsPerBase,
            maxNutrient,
            "CarbsPerBase exceeds the maximum for the selected unit");
        if (carbsResult.IsFailure) {
            return carbsResult;
        }

        Result fiberResult = ValidateMax(
            nameof(command.FiberPerBase),
            command.FiberPerBase ?? product.FiberPerBase,
            maxNutrient,
            "FiberPerBase exceeds the maximum for the selected unit");
        if (fiberResult.IsFailure) {
            return fiberResult;
        }

        return ValidateMax(
            nameof(command.AlcoholPerBase),
            command.AlcoholPerBase ?? product.AlcoholPerBase,
            maxNutrient,
            "AlcoholPerBase exceeds the maximum for the selected unit");
    }

    private static Result ValidateMax(string field, double value, double maxValue, string message) =>
        value <= maxValue
            ? Result.Success()
            : Result.Failure(Errors.Validation.Invalid(field, message));
}
