using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public class DeleteRecipeCommandHandler(
    IRecipeRepository recipeRepository,
    IImageAssetCleanupService imageAssetCleanupService)
    : ICommandHandler<DeleteRecipeCommand, Result> {
    public async Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var recipeId = new RecipeId(command.RecipeId);

        var recipe = await recipeRepository.GetByIdAsync(
            recipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (recipe is null) {
            return Result.Failure(Errors.Recipe.NotAccessible(command.RecipeId));
        }

        if (recipe.MealItems.Count + recipe.NestedRecipeUsages.Count > 0) {
            return Result.Failure(Errors.Validation.Invalid("RecipeId",
                "Recipe is already used and cannot be deleted"));
        }

        var assetId = recipe.ImageAssetId;
        var stepAssetIds = recipe.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        await recipeRepository.DeleteAsync(recipe, cancellationToken);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken);
        }

        foreach (var stepAssetId in stepAssetIds) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(stepAssetId, cancellationToken);
        }

        return Result.Success();
    }
}
