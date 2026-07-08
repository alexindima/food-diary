using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.Recipes.Commands.DeleteRecipe;

public sealed class DeleteRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteRecipeCommand, Result> {
    public async Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken) {
        Result<RecipeId> recipeIdResult = RequiredIdParser.Parse(
            command.RecipeId,
            nameof(command.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(recipeIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        RecipeId recipeId = recipeIdResult.Value;

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

        int usageCount = await recipeReadRepository.GetUsageCountAsync(
            recipe.Id,
            recipe.UserId,
            includePublic: false,
            cancellationToken).ConfigureAwait(false);
        if (usageCount > 0) {
            return Result.Failure(Errors.Validation.Invalid("RecipeId",
                "Recipe is already used and cannot be deleted"));
        }

        ImageAssetId? recipeAssetId = recipe.ImageAssetId;
        IReadOnlyList<ImageAssetId> stepAssetIds = GetStepAssetIds(recipe);
        await recipeWriteRepository.DeleteAsync(recipe, cancellationToken).ConfigureAwait(false);

        if (recipeAssetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(recipeAssetId.Value, cancellationToken).ConfigureAwait(false);
        }

        foreach (ImageAssetId stepAssetId in stepAssetIds) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(stepAssetId, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    private static IReadOnlyList<ImageAssetId> GetStepAssetIds(Recipe recipe) =>
        recipe.Steps
            .Select(step => step.ImageAssetId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
}
