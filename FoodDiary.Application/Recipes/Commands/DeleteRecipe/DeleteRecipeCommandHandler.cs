using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IImageAssetCleanupService imageAssetCleanupService)
    : ICommandHandler<DeleteRecipeCommand, Result> {
    public async Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.RecipeId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.RecipeId), "Recipe id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var recipeId = new RecipeId(command.RecipeId);

        Recipe? recipe = await recipeReadRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure(Errors.Recipe.NotAccessible(command.RecipeId));
        }

        if (recipe.MealItems.Count + recipe.NestedRecipeUsages.Count > 0) {
            return Result.Failure(Errors.Validation.Invalid("RecipeId",
                "Recipe is already used and cannot be deleted"));
        }

        ImageAssetId? assetId = recipe.ImageAssetId;
        var stepAssetIds = recipe.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        await recipeWriteRepository.DeleteAsync(recipe, cancellationToken).ConfigureAwait(false);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken).ConfigureAwait(false);
        }

        foreach (ImageAssetId stepAssetId in stepAssetIds) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(stepAssetId, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
