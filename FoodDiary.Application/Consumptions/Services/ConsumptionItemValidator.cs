using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Common;

namespace FoodDiary.Application.Consumptions.Services;

public static class ConsumptionItemValidator {
    public static Result Validate(ConsumptionItemInput item) {
        if (!item.ProductId.HasValue && !item.RecipeId.HasValue) {
            return Result.Failure(Errors.Validation.Invalid("Items", "Each item must contain productId or recipeId."));
        }

        if (item is { ProductId: not null, RecipeId: not null }) {
            return Result.Failure(Errors.Validation.Invalid("Items", "Item cannot contain both productId and recipeId."));
        }

        return item.Amount <= 0
            ? Result.Failure(Errors.Validation.Invalid("Amount", "Amount must be greater than zero."))
            : Result.Success();
    }
}
