using FoodDiary.Modules.Recipes.Domain.Entities;
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
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserDataPurgeParticipant, ITelegramDiaryContentTransfer {
    public int Order => 7;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        if (reassignTarget is { } target) {
            await TransferAsync(userId, target, cancellationToken);
            return;
        }

        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await RemovePublicationDataAsync(userId, cancellationToken);
        // Restrictive nested references are checked while rows are deleted. Remove
        // outgoing edges of this user's recipes first, including parent -> child.
        // Incoming references from another owner's recipes remain restrictive.
        await context.RecipeIngredients.Where(item => item.RecipeStep.Recipe.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.Recipes.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task TransferAsync(UserId sourceId, UserId targetId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        await RemovePublicationDataAsync(sourceId, cancellationToken);
        List<ImageAssetId> assetIds = await context.Recipes
            .Where(item => item.UserId == sourceId && item.ImageAssetId != null)
            .Select(item => item.ImageAssetId!.Value).ToListAsync(cancellationToken);
        assetIds.AddRange(await context.Recipes.Where(item => item.UserId == sourceId)
            .SelectMany(item => item.Images).Select(image => image.ImageAssetId)
            .ToListAsync(cancellationToken));

        List<ImageAssetId> stepAssets = await context.RecipeSteps
            .Where(step => step.Recipe.UserId == sourceId && step.ImageAssetId != null)
            .Select(step => step.ImageAssetId!.Value).ToListAsync(cancellationToken);
        stepAssets.AddRange(await context.RecipeSteps.Where(step => step.Recipe.UserId == sourceId)
            .SelectMany(step => step.Images).Select(image => image.ImageAssetId)
            .ToListAsync(cancellationToken));
        assetIds = [.. assetIds.Concat(stepAssets).Distinct()];

        await imageAssetOwnershipService.ReassignAsync(assetIds, sourceId, targetId, cancellationToken);
        await context.Recipes.Where(item => item.UserId == sourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.UserId, targetId), cancellationToken);
    }

    private async Task RemovePublicationDataAsync(UserId userId, CancellationToken cancellationToken) {
        await context.Set<RecipePublicationRevision>().Where(revision => revision.OwnerId == userId.Value)
            .ExecuteDeleteAsync(cancellationToken);
        await context.Set<RecipePublicationRevision>().Where(revision => revision.ReviewedBy == userId.Value)
            .ExecuteUpdateAsync(update => update.SetProperty(revision => revision.ReviewedBy, (Guid?)null), cancellationToken);
        await context.Set<RecipePublicationPolicy>().Where(policy => policy.PublishedBy == userId.Value)
            .ExecuteUpdateAsync(update => update.SetProperty(policy => policy.PublishedBy, (Guid?)null), cancellationToken);
    }
}
