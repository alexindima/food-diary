using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

        UserId? reassignTarget = null;
        if (reassignUserId.HasValue) {
            var candidate = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(
                    u => u.Id == new UserId(reassignUserId.Value) &&
                         u.DeletedAt == null &&
                         u.IsActive,
                    cancellationToken);
            if (candidate) {
                reassignTarget = new UserId(reassignUserId.Value);
            } else {
                logger.LogWarning(
                    "User cleanup reassign target {UserId} was not found or is not active. Proceeding without reassignment.",
                    reassignUserId);
            }
        }

        var thresholdUtc = olderThanUtc.Kind switch {
            DateTimeKind.Utc => olderThanUtc,
            DateTimeKind.Local => olderThanUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(olderThanUtc, DateTimeKind.Utc)
        };

        var userIds = await dbContext.Users
            .Where(u => u.DeletedAt != null && u.DeletedAt <= thresholdUtc)
            .OrderBy(u => u.DeletedAt)
            .Select(u => u.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var removed = 0;

        foreach (var userId in userIds) {
            try {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                if (reassignTarget is not null) {
                    var productAssetIds = await dbContext.Products
                        .Where(p => p.UserId == userId && p.ImageAssetId != null)
                        .Select(p => p.ImageAssetId!)
                        .ToListAsync(cancellationToken);

                    var recipeAssetIds = await dbContext.Recipes
                        .Where(r => r.UserId == userId && r.ImageAssetId != null)
                        .Select(r => r.ImageAssetId!)
                        .ToListAsync(cancellationToken);

                    var stepAssetIds = await dbContext.RecipeSteps
                        .Where(step => step.Recipe.UserId == userId && step.ImageAssetId != null)
                        .Select(step => step.ImageAssetId!)
                        .ToListAsync(cancellationToken);

                    var assetIds = productAssetIds
                        .Concat(recipeAssetIds)
                        .Concat(stepAssetIds)
                        .Distinct()
                        .ToList();

                    if (assetIds.Count > 0) {
                        await dbContext.ImageAssets
                            .Where(a => assetIds.Contains(a.Id))
                            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.UserId, reassignTarget), cancellationToken);
                    }

                    await dbContext.Products
                        .Where(p => p.UserId == userId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.UserId, reassignTarget), cancellationToken);

                    await dbContext.Recipes
                        .Where(r => r.UserId == userId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.UserId, reassignTarget), cancellationToken);
                } else {
                    await dbContext.Products
                        .Where(p => p.UserId == userId)
                        .ExecuteDeleteAsync(cancellationToken);

                    await dbContext.Recipes
                        .Where(r => r.UserId == userId)
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await dbContext.MealItems
                    .Where(item => item.Meal.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.Meals
                    .Where(meal => meal.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.ShoppingLists
                    .Where(list => list.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.RecentItems
                    .Where(item => item.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.HydrationEntries
                    .Where(entry => entry.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.WeightEntries
                    .Where(entry => entry.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.WaistEntries
                    .Where(entry => entry.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.CycleDays
                    .Where(day => day.Cycle.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.Cycles
                    .Where(cycle => cycle.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                var deletedImageObjectKeys = await dbContext.ImageAssets
                    .Where(asset => asset.UserId == userId)
                    .Select(asset => asset.ObjectKey)
                    .ToListAsync(cancellationToken);

                foreach (var objectKey in deletedImageObjectKeys) {
                    try {
                        await imageStorageService.DeleteAsync(objectKey, cancellationToken);
                    } catch (Exception ex) {
                        logger.LogWarning(ex, "Failed to delete image object {ObjectKey} during deleted user cleanup.", objectKey);
                    }
                }

                await dbContext.ImageAssets
                    .Where(asset => asset.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.AiUsages
                    .Where(usage => usage.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.UserRoles
                    .Where(role => role.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.Users
                    .Where(u => u.Id == userId)
                    .ExecuteDeleteAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                removed++;
            } catch (Exception ex) {
                logger.LogError(ex, "Failed to clean up deleted user {UserId}. Continuing with the next deleted user.", userId.Value);
            }
        }
        return removed;
    }
}
