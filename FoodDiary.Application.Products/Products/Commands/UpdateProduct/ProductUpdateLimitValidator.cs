using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

internal static class ProductUpdateLimitValidator {
    public static Result Validate(Product product, UpdateProductCommand command, ProductUpdateValues values) {
        MeasurementUnit unit = values.Unit ?? product.BaseUnit;
        double maxNutrient = Product.GetMaxNutrientPerBase(unit);

        LimitCheck[] checks = [
            new(
                nameof(command.DefaultPortionAmount),
                command.DefaultPortionAmount ?? product.DefaultPortionAmount,
                Product.GetMaxDefaultPortionAmount(unit),
                "DefaultPortionAmount exceeds the maximum for the selected unit"),
            new(
                nameof(command.CaloriesPerBase),
                command.CaloriesPerBase ?? product.CaloriesPerBase,
                Product.GetMaxCaloriesPerBase(unit),
                "CaloriesPerBase exceeds the maximum for the selected unit"),
            new(
                nameof(command.ProteinsPerBase),
                command.ProteinsPerBase ?? product.ProteinsPerBase,
                maxNutrient,
                "ProteinsPerBase exceeds the maximum for the selected unit"),
            new(
                nameof(command.FatsPerBase),
                command.FatsPerBase ?? product.FatsPerBase,
                maxNutrient,
                "FatsPerBase exceeds the maximum for the selected unit"),
            new(
                nameof(command.CarbsPerBase),
                command.CarbsPerBase ?? product.CarbsPerBase,
                maxNutrient,
                "CarbsPerBase exceeds the maximum for the selected unit"),
            new(
                nameof(command.FiberPerBase),
                command.FiberPerBase ?? product.FiberPerBase,
                maxNutrient,
                "FiberPerBase exceeds the maximum for the selected unit"),
            new(
                nameof(command.AlcoholPerBase),
                command.AlcoholPerBase ?? product.AlcoholPerBase,
                maxNutrient,
                "AlcoholPerBase exceeds the maximum for the selected unit"),
        ];

        LimitCheck? failure = checks.FirstOrDefault(static check => check.Value > check.MaxValue);
        return failure is null
            ? Result.Success()
            : Result.Failure(Errors.Validation.Invalid(failure.Field, failure.Message));
    }

    private sealed record LimitCheck(string Field, double Value, double MaxValue, string Message);
}
