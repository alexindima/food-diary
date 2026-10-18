using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.Infrastructure.Persistence;

internal sealed class RecipesUserDataPurgeParticipant(
    RecipesDbContext context,
    IModuleTransactionCoordinator coordinator,
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserDataPurgeParticipant {
    public int Order => 7;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        if (reassignTarget is { } target) {
            List<ImageAssetId> assetIds = await context.Recipes
                .Where(item => item.UserId == userId && item.ImageAssetId != null)
                .Select(item => item.ImageAssetId!.Value).ToListAsync(cancellationToken).ConfigureAwait(false);

            List<ImageAssetId> stepAssets = await context.RecipeSteps
                .Where(step => step.Recipe.UserId == userId && step.ImageAssetId != null)
                .Select(step => step.ImageAssetId!.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
            assetIds = [.. assetIds.Concat(stepAssets).Distinct()];

            await imageAssetOwnershipService.ReassignAsync(assetIds, target, cancellationToken).ConfigureAwait(false);
            await context.Recipes.Where(item => item.UserId == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, target), cancellationToken).ConfigureAwait(false);
        } else {
            // Restrictive nested references are checked while rows are deleted. Remove
            // outgoing edges of this user's recipes first, including parent -> child.
            // Incoming references from another owner's recipes remain restrictive.
            await context.RecipeIngredients.Where(item => item.RecipeStep.Recipe.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.Recipes.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
