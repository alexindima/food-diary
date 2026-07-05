using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

internal static class UpdateRecipeValuePreparer {
    public static async Task<Result<UpdateRecipeValues>> PrepareAsync(
        UpdateRecipeCommand command,
        IRecipeReadRepository recipeRepository,
        ICurrentUserAccessService currentUserAccessService,
        IImageAssetAccessService imageAssetAccessService,
        IProductLookupService productLookupService,
        IRecipeLookupService recipeLookupService,
        CancellationToken cancellationToken) {
        Result commandValidation = ValidateCommand(command);
        if (commandValidation.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(commandValidation.Error);
        }

        Result<ImageAssetId?> imageAssetIdResult = ImageAssetIdParser.ParseOptional(command.ImageAssetId, nameof(command.ImageAssetId));
        if (imageAssetIdResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(imageAssetIdResult.Error);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<UpdateRecipeValues>(accessError);
        }

        var recipeId = new RecipeId(command.RecipeId);
        Result<Recipe> recipeResult = await ResolveEditableRecipeAsync(command, recipeId, userId, recipeRepository, cancellationToken).ConfigureAwait(false);
        if (recipeResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(recipeResult.Error);
        }

        Result<Visibility?> visibilityResult = ParseVisibility(command);
        if (visibilityResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(visibilityResult.Error);
        }

        IReadOnlyList<RecipeStepInput> steps = command.Steps ?? Array.Empty<RecipeStepInput>();
        Result ingredientAccessResult = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            steps,
            recipeId,
            userId,
            productLookupService,
            recipeLookupService,
            cancellationToken).ConfigureAwait(false);
        if (ingredientAccessResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(ingredientAccessResult.Error);
        }

        Result<ImageAsset?> imageAssetResult = await ResolveImageAssetAsync(
            imageAssetIdResult.Value,
            userId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (imageAssetResult.IsFailure) {
            return Result.Failure<UpdateRecipeValues>(imageAssetResult.Error);
        }

        Recipe recipe = recipeResult.Value;
        return Result.Success(new UpdateRecipeValues(
            userId,
            recipeId,
            recipe,
            visibilityResult.Value,
            imageAssetIdResult.Value,
            imageAssetResult.Value,
            recipe.ImageAssetId,
            GetStepAssetIds(recipe),
            steps));
    }

    private static Task<Result<ImageAsset?>> ResolveImageAssetAsync(
        ImageAssetId? imageAssetId,
        UserId userId,
        IImageAssetAccessService imageAssetAccessService,
        CancellationToken cancellationToken) =>
        imageAssetAccessService.ResolveOptionalAsync(imageAssetId, userId, cancellationToken);

    private static IReadOnlyList<ImageAssetId> GetStepAssetIds(Recipe recipe) =>
        recipe.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

    private static Result ValidateCommand(UpdateRecipeCommand command) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        return command.RecipeId == Guid.Empty
            ? Result.Failure(Errors.Validation.Invalid(nameof(command.RecipeId), "Recipe id must not be empty."))
            : Result.Success();
    }

    private static async Task<Result<Recipe>> ResolveEditableRecipeAsync(
        UpdateRecipeCommand command,
        RecipeId recipeId,
        UserId userId,
        IRecipeReadRepository recipeRepository,
        CancellationToken cancellationToken) {
        Recipe? recipe = await recipeRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (recipe is null) {
            return Result.Failure<Recipe>(Errors.Recipe.NotAccessible(command.RecipeId));
        }

        int usageCount = await recipeRepository.GetUsageCountAsync(
            recipe.Id,
            recipe.UserId,
            includePublic: false,
            cancellationToken).ConfigureAwait(false);
        return usageCount > 0
            ? Result.Failure<Recipe>(
                Errors.Validation.Invalid("RecipeId", "Recipe is already used and cannot be modified"))
            : Result.Success(recipe);
    }

    private static Result<Visibility?> ParseVisibility(UpdateRecipeCommand command) {
        if (string.IsNullOrWhiteSpace(command.Visibility)) {
            return Result.Success<Visibility?>(value: null);
        }

        return EnumValueParser.ParseOptional<Visibility>(
            command.Visibility,
            nameof(command.Visibility),
            "Unknown visibility value.");
    }
}
