using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
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
        Result<IReadOnlyList<ProductId>> productIdsResult = ParseProductIds(steps);
        if (productIdsResult.IsFailure) {
            return Result.Failure(productIdsResult.Error);
        }

        IReadOnlyList<ProductId> productIds = productIdsResult.Value;

        if (productIds.Count > 0) {
            IReadOnlyDictionary<ProductId, Product> products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken).ConfigureAwait(false);
            if (products.Count != productIds.Count) {
                return Result.Failure(Errors.Validation.Invalid(
                    nameof(RecipeIngredientInput.ProductId),
                    "Product not found or you do not have access to it."));
            }
        }

        Result<IReadOnlyList<RecipeId>> nestedRecipeIdsResult = ParseNestedRecipeIds(steps);
        if (nestedRecipeIdsResult.IsFailure) {
            return Result.Failure(nestedRecipeIdsResult.Error);
        }

        IReadOnlyList<RecipeId> nestedRecipeIds = nestedRecipeIdsResult.Value;

        if (recipeId.HasValue && nestedRecipeIds.Contains(recipeId.Value)) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(RecipeIngredientInput.NestedRecipeId),
                "Recipe cannot contain itself as an ingredient."));
        }

        if (nestedRecipeIds.Count == 0) {
            return Result.Success();
        }

        IReadOnlyDictionary<RecipeId, Recipe> recipes = await recipeLookupService.GetAccessibleByIdsAsync(nestedRecipeIds, userId, cancellationToken).ConfigureAwait(false);
        return recipes.Count == nestedRecipeIds.Count
            ? Result.Success()
            : Result.Failure(Errors.Validation.Invalid(
                nameof(RecipeIngredientInput.NestedRecipeId),
                "Nested recipe not found or you do not have access to it."));
    }

    private static Result<IReadOnlyList<ProductId>> ParseProductIds(IReadOnlyList<RecipeStepInput> steps) =>
        ParseIngredientIds(
            steps,
            ingredient => ingredient.ProductId,
            nameof(RecipeIngredientInput.ProductId),
            "Product id",
            value => new ProductId(value));

    private static Result<IReadOnlyList<RecipeId>> ParseNestedRecipeIds(IReadOnlyList<RecipeStepInput> steps) =>
        ParseIngredientIds(
            steps,
            ingredient => ingredient.NestedRecipeId,
            nameof(RecipeIngredientInput.NestedRecipeId),
            "Nested recipe id",
            value => new RecipeId(value));

    private static Result<IReadOnlyList<TId>> ParseIngredientIds<TId>(
        IReadOnlyList<RecipeStepInput> steps,
        Func<RecipeIngredientInput, Guid?> getId,
        string fieldName,
        string displayName,
        Func<Guid, TId> createId) where TId : struct {
        var ids = new HashSet<TId>();
        foreach (RecipeIngredientInput ingredient in steps.SelectMany(step => step.Ingredients)) {
            Result<TId?> idResult = OptionalEntityIdValidator.Parse(getId(ingredient), fieldName, displayName, createId);
            if (idResult.IsFailure) {
                return Result.Failure<IReadOnlyList<TId>>(idResult.Error);
            }

            if (idResult.Value.HasValue) {
                ids.Add(idResult.Value.Value);
            }
        }

        return Result.Success<IReadOnlyList<TId>>(ids.ToList());
    }
}
