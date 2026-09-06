using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class RecipesUserDataPurgeParticipant(
    FoodDiaryDbContext context,
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserDataPurgeParticipant {
    public int Order => 20;

    public async Task PurgeAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
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
            await context.Recipes.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
