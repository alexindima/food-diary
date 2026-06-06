using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Consumptions.Services;

public static class ConsumptionItemValidator {
    private const double MaxAmount = 1_000_000d;

    public static Result Validate(ConsumptionItemInput item) {
        if (!item.ProductId.HasValue && !item.RecipeId.HasValue) {
            return Result.Failure(Errors.Validation.Invalid("Items", "Each item must contain productId or recipeId."));
        }

        if (item is { ProductId: not null, RecipeId: not null }) {
            return Result.Failure(Errors.Validation.Invalid("Items", "Item cannot contain both productId and recipeId."));
        }

        if (double.IsNaN(item.Amount) || double.IsInfinity(item.Amount)) {
            return Result.Failure(Errors.Validation.Invalid("Amount", "Amount must be a finite number."));
        }

        return item.Amount <= 0 || item.Amount > MaxAmount
            ? Result.Failure(Errors.Validation.Invalid("Amount", string.Create(CultureInfo.InvariantCulture, $"Amount must be in range (0, {MaxAmount}].")))
            : Result.Success();
    }
}
