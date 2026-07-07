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

            Result<ProductId?> productIdResult = OptionalEntityIdValidator.Parse(
                item.ProductId,
                nameof(item.ProductId),
                "Product id",
                value => new ProductId(value));
            if (productIdResult.IsFailure) {
                return productIdResult;
            }

            Result<RecipeId?> recipeIdResult = OptionalEntityIdValidator.Parse(
                item.RecipeId,
                nameof(item.RecipeId),
                "Recipe id",
                value => new RecipeId(value));
            if (recipeIdResult.IsFailure) {
                return recipeIdResult;
            }

            if (productIdResult.Value.HasValue) {
                MealItem mealItem = meal.AddProduct(productIdResult.Value.Value, item.Amount);
                Result sourceResult = ApplySource(mealItem, item);
                if (sourceResult.IsFailure) {
                    return sourceResult;
                }
            } else if (recipeIdResult.Value.HasValue) {
                MealItem mealItem = meal.AddRecipe(recipeIdResult.Value.Value, item.Amount);
                Result sourceResult = ApplySource(mealItem, item);
                if (sourceResult.IsFailure) {
                    return sourceResult;
                }
            }
        }

        return Result.Success();
    }

    private static Result ApplySource(MealItem mealItem, ConsumptionItemInput item) {
        if (!TryParseMealItemOrigin(item.Origin, out MealItemOrigin origin)) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(item.Origin), "Unknown meal item origin value."));
        }

        Result<MealAiItemId?> sourceAiItemIdResult = OptionalEntityIdValidator.Parse(
            item.SourceAiItemId,
            nameof(item.SourceAiItemId),
            "Source AI item id",
            value => new MealAiItemId(value));
        if (sourceAiItemIdResult.IsFailure) {
            return sourceAiItemIdResult;
        }

        try {
            mealItem.ApplySource(sourceAiItemIdResult.Value, origin);
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
