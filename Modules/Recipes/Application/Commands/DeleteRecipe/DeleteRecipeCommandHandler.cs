using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Contracts.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;

namespace FoodDiary.Modules.Recipes.Application.Commands.DeleteRecipe;

public sealed class DeleteRecipeCommandHandler(
    IRecipeReadRepository recipeReadRepository,
    IRecipeWriteRepository recipeWriteRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService,
    IRecipeMutationTransactionRunner transactionRunner)
    : ICommandHandler<DeleteRecipeCommand, Result> {
    public Task<Result> Handle(DeleteRecipeCommand command, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync(
            token => HandleCoreAsync(command, token),
            cancellationToken);

    private async Task<Result> HandleCoreAsync(DeleteRecipeCommand command, CancellationToken cancellationToken) {
        Result<RecipeId> recipeIdResult = RecipeRequiredIdParser.Parse(
            command.RecipeId,
            nameof(command.RecipeId),
            "Recipe id must not be empty.",
            value => new RecipeId(value));
        if (recipeIdResult.IsFailure) {
            return RecipeRequiredIdParser.ToFailure(recipeIdResult);
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

        Recipe? recipe = await recipeReadRepository.GetByIdForUpdateAsync(
            recipeId,
            userId,
            includePublic: false,
            includeSteps: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (recipe is null) {
            return Result.Failure(RecipeErrors.NotAccessible(command.RecipeId));
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
