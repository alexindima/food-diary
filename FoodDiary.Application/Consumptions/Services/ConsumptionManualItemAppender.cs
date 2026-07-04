using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Services;

internal static class ConsumptionManualItemAppender {
    public static Result Add(Meal meal, IEnumerable<ConsumptionItemInput> items) {
        foreach (ConsumptionItemInput item in items) {
            Result validation = ConsumptionItemValidator.Validate(item);
            if (validation.IsFailure) {
                return validation;
            }

            Result itemIdValidation = ValidateItemIdentifiers(item);
            if (itemIdValidation.IsFailure) {
                return itemIdValidation;
            }

            if (item.ProductId.HasValue) {
                MealItem mealItem = meal.AddProduct(new ProductId(item.ProductId.Value), item.Amount);
                Result sourceResult = ApplySource(mealItem, item);
                if (sourceResult.IsFailure) {
                    return sourceResult;
                }
            } else if (item.RecipeId.HasValue) {
                MealItem mealItem = meal.AddRecipe(new RecipeId(item.RecipeId.Value), item.Amount);
                Result sourceResult = ApplySource(mealItem, item);
                if (sourceResult.IsFailure) {
                    return sourceResult;
                }
            }
        }

        return Result.Success();
    }

    private static Result ValidateItemIdentifiers(ConsumptionItemInput item) {
        Result productIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.ProductId, nameof(item.ProductId), "Product id");
        if (productIdResult.IsFailure) {
            return productIdResult;
        }

        Result recipeIdResult = OptionalEntityIdValidator.EnsureNotEmpty(item.RecipeId, nameof(item.RecipeId), "Recipe id");
        return recipeIdResult.IsFailure ? recipeIdResult : Result.Success();
    }

    private static Result ApplySource(MealItem mealItem, ConsumptionItemInput item) {
        if (!TryParseMealItemOrigin(item.Origin, out MealItemOrigin origin)) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(item.Origin), "Unknown meal item origin value."));
        }

        if (item.SourceAiItemId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(item.SourceAiItemId), "Source AI item id must not be empty."));
        }

        try {
            MealAiItemId? sourceAiItemId = item.SourceAiItemId.HasValue
                ? new MealAiItemId(item.SourceAiItemId.Value)
                : null;
            mealItem.ApplySource(sourceAiItemId, origin);
        } catch (ArgumentException ex) {
            return Result.Failure(Errors.Validation.Invalid("Items", ex.Message));
        }

        return Result.Success();
    }

    private static bool TryParseMealItemOrigin(string? origin, out MealItemOrigin result) {
        if (!string.IsNullOrWhiteSpace(origin)) {
            return EnumValueParser.TryParse(origin, out result);
        }

        result = MealItemOrigin.Manual;
        return true;
    }
}
