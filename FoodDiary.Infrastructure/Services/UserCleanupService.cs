using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Services;

public sealed class UserCleanupService(
    FoodDiaryDbContext dbContext,
    IImageStorageService imageStorageService,
    ILogger<UserCleanupService> logger) : IUserCleanupService {
    public async Task<int> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");
        }

        UserId? reassignTarget = await ResolveReassignTargetAsync(reassignUserId, cancellationToken).ConfigureAwait(false);
        DateTime thresholdUtc = NormalizeUtc(olderThanUtc);
        IReadOnlyList<UserId> userIds = await GetDeletedUserIdsAsync(thresholdUtc, batchSize, cancellationToken).ConfigureAwait(false);
        int removed = 0;

        foreach (UserId userId in userIds) {
            try {
                await CleanupUserAsync(userId, reassignTarget, cancellationToken).ConfigureAwait(false);
                removed++;
            } catch (Exception ex) {
                logger.LogError(ex, "Failed to clean up deleted user {UserId}. Continuing with the next deleted user.", userId.Value);
            }
        }
        return removed;
    }

    private async Task<UserId?> ResolveReassignTargetAsync(Guid? reassignUserId, CancellationToken cancellationToken) {
        if (!reassignUserId.HasValue) {
            return null;
        }

        var targetId = new UserId(reassignUserId.Value);
        bool candidate = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == targetId && u.DeletedAt == null && u.IsActive,
                cancellationToken).ConfigureAwait(false);
        if (candidate) {
            return targetId;
        }

        logger.LogWarning(
            "User cleanup reassign target {UserId} was not found or is not active. Proceeding without reassignment.",
            reassignUserId);
        return null;
    }

    private static DateTime NormalizeUtc(DateTime value) {
        return value.Kind switch {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private async Task<IReadOnlyList<UserId>> GetDeletedUserIdsAsync(DateTime thresholdUtc, int batchSize, CancellationToken cancellationToken) {
        return await dbContext.Users
            .Where(u => u.DeletedAt != null && u.DeletedAt <= thresholdUtc)
            .OrderBy(u => u.DeletedAt)
            .Select(u => u.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task CleanupUserAsync(UserId userId, UserId? reassignTarget, CancellationToken cancellationToken) {
        IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false)) {
            if (reassignTarget is not null) {
                await ReassignOwnedContentAsync(userId, reassignTarget.Value, cancellationToken).ConfigureAwait(false);
            } else {
                await DeleteOwnedContentAsync(userId, cancellationToken).ConfigureAwait(false);
            }

            await DeleteDependentRowsAsync(userId, cancellationToken).ConfigureAwait(false);
            await DeleteImageAssetsAsync(userId, cancellationToken).ConfigureAwait(false);
            await DeleteUserRowsAsync(userId, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ReassignOwnedContentAsync(UserId userId, UserId reassignTarget, CancellationToken cancellationToken) {
        IReadOnlyList<ImageAssetId> assetIds = await GetOwnedContentAssetIdsAsync(userId, cancellationToken).ConfigureAwait(false);
        if (assetIds.Count > 0) {
            await dbContext.ImageAssets
                .Where(a => assetIds.Contains(a.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.UserId, reassignTarget), cancellationToken).ConfigureAwait(false);
        }

        await dbContext.Products
            .Where(p => p.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.UserId, reassignTarget), cancellationToken).ConfigureAwait(false);

        await dbContext.Recipes
            .Where(r => r.UserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.UserId, reassignTarget), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ImageAssetId>> GetOwnedContentAssetIdsAsync(UserId userId, CancellationToken cancellationToken) {
        List<ImageAssetId> productAssetIds = await dbContext.Products
            .Where(p => p.UserId == userId && p.ImageAssetId != null)
            .Select(p => p.ImageAssetId!.Value)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<ImageAssetId> recipeAssetIds = await dbContext.Recipes
            .Where(r => r.UserId == userId && r.ImageAssetId != null)
            .Select(r => r.ImageAssetId!.Value)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        List<ImageAssetId> stepAssetIds = await dbContext.RecipeSteps
            .Where(step => step.Recipe.UserId == userId && step.ImageAssetId != null)
            .Select(step => step.ImageAssetId!.Value)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return productAssetIds
            .Concat(recipeAssetIds)
            .Concat(stepAssetIds)
            .Distinct()
            .ToList();
    }

    private async Task DeleteOwnedContentAsync(UserId userId, CancellationToken cancellationToken) {
        await dbContext.Products
            .Where(p => p.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

        await dbContext.Recipes
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteDependentRowsAsync(UserId userId, CancellationToken cancellationToken) {
        await dbContext.MealItems.Where(item => item.Meal.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Meals.Where(meal => meal.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.ShoppingLists.Where(list => list.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.RecentItems.Where(item => item.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.HydrationEntries.Where(entry => entry.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.WeightEntries.Where(entry => entry.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.WaistEntries.Where(entry => entry.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.CycleDays.Where(day => day.Cycle.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Cycles.Where(cycle => cycle.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteImageAssetsAsync(UserId userId, CancellationToken cancellationToken) {
        List<string> deletedImageObjectKeys = await dbContext.ImageAssets
            .Where(asset => asset.UserId == userId)
            .Select(asset => asset.ObjectKey)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        foreach (string objectKey in deletedImageObjectKeys) {
            await DeleteImageObjectAsync(objectKey, cancellationToken).ConfigureAwait(false);
        }

        await dbContext.ImageAssets
            .Where(asset => asset.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DeleteImageObjectAsync(string objectKey, CancellationToken cancellationToken) {
        try {
            await imageStorageService.DeleteAsync(objectKey, cancellationToken).ConfigureAwait(false);
        } catch (Exception ex) {
            logger.LogWarning(ex, "Failed to delete image object {ObjectKey} during deleted user cleanup.", objectKey);
        }
    }

    private async Task DeleteUserRowsAsync(UserId userId, CancellationToken cancellationToken) {
        await dbContext.AiUsages.Where(usage => usage.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.UserRoles.Where(role => role.UserId == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        await dbContext.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }
}
