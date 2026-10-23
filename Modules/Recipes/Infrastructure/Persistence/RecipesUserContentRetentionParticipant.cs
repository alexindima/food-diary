using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.Infrastructure.Persistence;

internal sealed class RecipesUserContentRetentionParticipant(
    RecipesDbContext context,
    IModuleTransactionCoordinator coordinator,
    IRecipeUserPurgeRetentionReadService retentionReadService,
    IImageAssetOwnershipService imageAssetOwnershipService) : IUserContentRetentionParticipant {
    public int Order => 7;

    public async Task LockOwnedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        FormattableString sql = $"""
            SELECT "Id" AS "Value"
            FROM "Recipes"
            WHERE "UserId" = {sourceUserId.Value}
            ORDER BY "Id"
            FOR UPDATE
            """;
        _ = await context.Database.SqlQuery<Guid>(sql).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasReferencedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        IReadOnlyCollection<RecipeId> recipeIds = await retentionReadService
            .GetRetainedRecipeIdsAsync(sourceUserId, cancellationToken).ConfigureAwait(false);
        return recipeIds.Count > 0;
    }

    public async Task RetainReferencedContentAsync(UserId sourceUserId, UserId targetUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<RecipeId> recipeIds = await retentionReadService
            .GetRetainedRecipeIdsAsync(sourceUserId, cancellationToken).ConfigureAwait(false);
        if (recipeIds.Count == 0) {
            return;
        }

        IQueryable<Recipe> recipes = context.Recipes.Where(recipe => recipe.UserId == sourceUserId && recipeIds.Contains(recipe.Id));
        IQueryable<RecipeStep> steps = context.RecipeSteps.Where(step => step.Recipe.UserId == sourceUserId && recipeIds.Contains(step.RecipeId));
        List<ImageAssetId> imageIds = await recipes.Where(recipe => recipe.ImageAssetId != null)
            .Select(recipe => recipe.ImageAssetId!.Value).ToListAsync(cancellationToken).ConfigureAwait(false);
        imageIds.AddRange(await recipes.SelectMany(recipe => recipe.Images).Select(image => image.ImageAssetId)
            .ToListAsync(cancellationToken).ConfigureAwait(false));
        imageIds.AddRange(await steps.Where(step => step.ImageAssetId != null)
            .Select(step => step.ImageAssetId!.Value).ToListAsync(cancellationToken).ConfigureAwait(false));
        imageIds.AddRange(await steps.SelectMany(step => step.Images).Select(image => image.ImageAssetId)
            .ToListAsync(cancellationToken).ConfigureAwait(false));

        await imageAssetOwnershipService.ReassignAsync([.. imageIds.Distinct()], sourceUserId, targetUserId, cancellationToken).ConfigureAwait(false);
        await recipes.ExecuteUpdateAsync(setters => setters.SetProperty(recipe => recipe.UserId, targetUserId), cancellationToken).ConfigureAwait(false);
    }
}
