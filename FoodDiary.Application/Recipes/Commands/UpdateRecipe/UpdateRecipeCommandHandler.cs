using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.UpdateRecipe;

public sealed class UpdateRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IRecipeNutritionWriter recipeNutritionWriter,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService,
    IImageAssetAccessService imageAssetAccessService,
    IProductLookupService productLookupService,
    IRecipeLookupService recipeLookupService)
    : ICommandHandler<UpdateRecipeCommand, Result<RecipeModel>> {
    public async Task<Result<RecipeModel>> Handle(UpdateRecipeCommand command, CancellationToken cancellationToken) {
        Result<UpdateRecipeValues> valuesResult = await UpdateRecipeValuePreparer.PrepareAsync(
            command,
            recipeReadRepository,
            currentUserAccessService,
            imageAssetAccessService,
            productLookupService,
            recipeLookupService,
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<RecipeModel>(valuesResult.Error);
        }

        UpdateRecipeValues values = valuesResult.Value;
        RecipeUpdateApplier.Apply(values.Recipe, command, values);

        Result stepsResult = await RecipeStepAppender.ReplaceAsync(
            values.Recipe,
            values.Steps,
            values.UserId,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (stepsResult.IsFailure) {
            return Result.Failure<RecipeModel>(stepsResult.Error);
        }

        Result nutritionResult = RecipeNutritionApplier.Apply(
            values.Recipe,
            command.CalculateNutritionAutomatically,
            command.ManualCalories,
            command.ManualProteins,
            command.ManualFats,
            command.ManualCarbs,
            command.ManualFiber,
            command.ManualAlcohol);
        if (nutritionResult.IsFailure) {
            return Result.Failure<RecipeModel>(nutritionResult.Error);
        }

        Result<Recipe> updatedResult = await SaveAndReloadAsync(values.Recipe, values.UserId, cancellationToken).ConfigureAwait(false);
        if (updatedResult.IsFailure) {
            return Result.Failure<RecipeModel>(updatedResult.Error);
        }

        await RecipeUpdateAssetCleanup.DeleteUnusedAsync(
            command,
            values,
            imageAssetCleanupService,
            cancellationToken).ConfigureAwait(false);
        Recipe updated = updatedResult.Value;
        return Result.Success(updated.ToModel(usageCount: 0, isOwnedByCurrentUser: true));
    }

    private async Task<Result<Recipe>> SaveAndReloadAsync(
        Recipe recipe,
        UserId userId,
        CancellationToken cancellationToken) {
        await recipeWriteRepository.UpdateAsync(recipe, cancellationToken).ConfigureAwait(false);

        Recipe? updated = await recipeReadRepository.GetByIdAsync(
            recipe.Id,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (updated is null) {
            return Result.Failure<Recipe>(Errors.Recipe.InvalidData("Failed to load updated recipe."));
        }

        await RecipeNutritionUpdater.EnsureNutritionAsync(updated, recipeNutritionWriter, cancellationToken).ConfigureAwait(false);
        return Result.Success(updated);
    }

}
