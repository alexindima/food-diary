using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Services;

internal static class RecipeStepAppender {
    public static Task<Result> AddAsync(
        Recipe recipe,
        IReadOnlyList<RecipeStepInput> steps,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) =>
        AppendAsync(recipe, steps, userId, imageAssetAccessService, clearExistingSteps: false, cancellationToken);

    public static Task<Result> ReplaceAsync(
        Recipe recipe,
        IReadOnlyList<RecipeStepInput> steps,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) =>
        AppendAsync(recipe, steps, userId, imageAssetAccessService, clearExistingSteps: true, cancellationToken);

    private static async Task<Result> AppendAsync(
        Recipe recipe,
        IReadOnlyList<RecipeStepInput> steps,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        bool clearExistingSteps,
        CancellationToken cancellationToken) {
        if (clearExistingSteps) {
            recipe.ClearSteps();
        }

        var orderedSteps = steps
            .Select((step, index) => new { Step = step, Order = step.Order > 0 ? step.Order : index + 1 })
            .OrderBy(x => x.Order)
            .ToList();

        foreach (var entry in orderedSteps) {
            Result<ImageAssetId?> stepImageAssetIdResult = ImageAssetIdParser.ParseOptional(entry.Step.ImageAssetId, nameof(entry.Step.ImageAssetId));
            if (stepImageAssetIdResult.IsFailure) {
                return stepImageAssetIdResult;
            }

            Result<ImageAsset?> stepImageAssetResult = await imageAssetAccessService.ResolveOptionalAsync(
                stepImageAssetIdResult.Value,
                userId,
                cancellationToken).ConfigureAwait(false);
            if (stepImageAssetResult.IsFailure) {
                return stepImageAssetResult;
            }

            RecipeStep step = recipe.AddStep(
                entry.Order,
                entry.Step.Description,
                entry.Step.Title,
                stepImageAssetResult.Value?.Url ?? entry.Step.ImageUrl,
                stepImageAssetIdResult.Value);

            Result ingredientsResult = AddIngredients(step, entry.Step.Ingredients);
            if (ingredientsResult.IsFailure) {
                return ingredientsResult;
            }
        }

        return Result.Success();
    }

    private static Result AddIngredients(RecipeStep step, IEnumerable<RecipeIngredientInput> ingredients) {
        foreach (RecipeIngredientInput ingredient in ingredients) {
            Result<ProductId?> productIdResult = OptionalEntityIdValidator.Parse(
                ingredient.ProductId,
                nameof(ingredient.ProductId),
                "Product id",
                value => new ProductId(value));
            if (productIdResult.IsFailure) {
                return productIdResult;
            }

            Result<RecipeId?> nestedRecipeIdResult = OptionalEntityIdValidator.Parse(
                ingredient.NestedRecipeId,
                nameof(ingredient.NestedRecipeId),
                "Nested recipe id",
                value => new RecipeId(value));
            if (nestedRecipeIdResult.IsFailure) {
                return nestedRecipeIdResult;
            }

            if (productIdResult.Value.HasValue) {
                step.AddProductIngredient(productIdResult.Value.Value, ingredient.Amount);
            } else if (nestedRecipeIdResult.Value.HasValue) {
                step.AddNestedRecipeIngredient(nestedRecipeIdResult.Value.Value, ingredient.Amount);
            }
        }

        return Result.Success();
    }
}
