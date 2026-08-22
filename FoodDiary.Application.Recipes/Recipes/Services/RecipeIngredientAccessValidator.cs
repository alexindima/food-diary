using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Recipes.Models;
using FoodDiary.Application.Recipes.Recipes.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Recipes.Services;

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
            IReadOnlyDictionary<ProductId, ProductOverviewReadItem> products = await productLookupService.GetAccessibleByIdsAsync(productIds, userId, cancellationToken).ConfigureAwait(false);
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

        IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> recipes = await recipeLookupService.GetAccessibleByIdsAsync(nestedRecipeIds, userId, cancellationToken).ConfigureAwait(false);
        if (recipes.Count != nestedRecipeIds.Count) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(RecipeIngredientInput.NestedRecipeId),
                "Nested recipe not found or you do not have access to it."));
        }

        return recipeId.HasValue
            ? await EnsureNoIndirectCycleAsync(recipeId.Value, recipes.Values, userId, recipeLookupService, cancellationToken).ConfigureAwait(false)
            : Result.Success();
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static async Task<Result> EnsureNoIndirectCycleAsync(
        RecipeId recipeId,
        IEnumerable<RecipeOverviewReadItem> initialRecipes,
        UserId userId,
        IRecipeLookupService recipeLookupService,
        CancellationToken cancellationToken) {
        var visited = new HashSet<RecipeId>();
        IReadOnlyCollection<RecipeOverviewReadItem> currentRecipes = [.. initialRecipes];

        while (currentRecipes.Count > 0) {
            var nextIds = new HashSet<RecipeId>();
            foreach (RecipeOverviewReadItem recipe in currentRecipes) {
                if (!visited.Add(recipe.Id)) {
                    continue;
                }

                foreach (Guid nestedIdValue in recipe.Steps
                    .SelectMany(step => step.Ingredients)
                    .Where(ingredient => ingredient.NestedRecipeId.HasValue)
                    .Select(ingredient => ingredient.NestedRecipeId!.Value)) {
                    var nestedId = new RecipeId(nestedIdValue);
                    if (nestedId == recipeId) {
                        return Result.Failure(Errors.Validation.Invalid(
                            nameof(RecipeIngredientInput.NestedRecipeId),
                            "Nested recipes would create a circular dependency."));
                    }

                    if (!visited.Contains(nestedId)) {
                        nextIds.Add(nestedId);
                    }
                }
            }

            if (nextIds.Count == 0) {
                return Result.Success();
            }

            IReadOnlyDictionary<RecipeId, RecipeOverviewReadItem> nextRecipes = await recipeLookupService
                .GetAccessibleByIdsAsync(nextIds, userId, cancellationToken)
                .ConfigureAwait(false);
            if (nextRecipes.Count != nextIds.Count) {
                return Result.Failure(Errors.Validation.Invalid(
                    nameof(RecipeIngredientInput.NestedRecipeId),
                    "Nested recipe dependency not found or you do not have access to it."));
            }

            currentRecipes = [.. nextRecipes.Values];
        }

        return Result.Success();
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
            Result<TId?> idResult = RecipeOptionalEntityIdParser.Parse(getId(ingredient), fieldName, displayName, createId);
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
