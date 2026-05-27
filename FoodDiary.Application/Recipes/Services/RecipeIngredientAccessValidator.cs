using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Services;

public static class RecipeIngredientAccessValidator {
    public static async Task<Result> EnsureIngredientsAccessibleAsync(
        IReadOnlyList<RecipeStepInput> steps,
        RecipeId? recipeId,
        UserId userId,
        IProductLookupService productLookupService,
        IRecipeLookupService recipeLookupService,
        CancellationToken cancellationToken) {
        var productIds = steps
            .SelectMany(step => step.Ingredients)
            .Where(ingredient => ingredient.ProductId.HasValue)
            .Select(ingredient => new ProductId(ingredient.ProductId!.Value))
            .Distinct()
            .ToList();

        if (productIds.Count > 0) {
            var products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken);
            if (products.Count != productIds.Count) {
                return Result.Failure(Errors.Validation.Invalid(
                    nameof(RecipeIngredientInput.ProductId),
                    "Product not found or you do not have access to it."));
            }
        }

        var nestedRecipeIds = steps
            .SelectMany(step => step.Ingredients)
            .Where(ingredient => ingredient.NestedRecipeId.HasValue)
            .Select(ingredient => new RecipeId(ingredient.NestedRecipeId!.Value))
            .Distinct()
            .ToList();

        if (recipeId.HasValue && nestedRecipeIds.Contains(recipeId.Value)) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(RecipeIngredientInput.NestedRecipeId),
                "Recipe cannot contain itself as an ingredient."));
        }

        if (nestedRecipeIds.Count == 0) {
            return Result.Success();
        }

        var recipes = await recipeLookupService.GetAccessibleByIdsAsync(nestedRecipeIds, userId, cancellationToken);
        return recipes.Count == nestedRecipeIds.Count
            ? Result.Success()
            : Result.Failure(Errors.Validation.Invalid(
                nameof(RecipeIngredientInput.NestedRecipeId),
                "Nested recipe not found or you do not have access to it."));
    }
}
