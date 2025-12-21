using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Services;

public sealed class UserCleanupService(
    FoodDiaryDbContext dbContext,
    ILogger<UserCleanupService> logger) : IUserCleanupService
{
    public async Task<int> CleanupDeletedUsersAsync(
        DateTime olderThanUtc,
        int batchSize,
        Guid? reassignUserId,
        CancellationToken cancellationToken = default)
    {
        UserId? reassignTarget = null;
        if (reassignUserId.HasValue)
        {
            var candidate = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == new UserId(reassignUserId.Value), cancellationToken);
            if (candidate)
            {
                reassignTarget = new UserId(reassignUserId.Value);
            }
            else
            {
                logger.LogWarning("User cleanup reassign target {UserId} was not found. Proceeding without reassignment.", reassignUserId);
            }
        }

        var userIds = await dbContext.Users
            .Where(u => u.DeletedAt != null && u.DeletedAt <= olderThanUtc)
            .OrderBy(u => u.DeletedAt)
            .Select(u => u.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var removed = 0;

        foreach (var userId in userIds)
        {
            if (reassignTarget is not null)
            {
                var productAssetIds = await dbContext.Products
                    .Where(p => p.UserId == userId && p.ImageAssetId != null)
                    .Select(p => p.ImageAssetId!)
                    .ToListAsync(cancellationToken);

                var recipeAssetIds = await dbContext.Recipes
                    .Where(r => r.UserId == userId && r.ImageAssetId != null)
                    .Select(r => r.ImageAssetId!)
                    .ToListAsync(cancellationToken);

                var assetIds = productAssetIds
                    .Concat(recipeAssetIds)
                    .Distinct()
                    .ToList();

                if (assetIds.Count > 0)
                {
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
            }
            else
            {
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

            await dbContext.ImageAssets
                .Where(asset => asset.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.Users
                .Where(u => u.Id == userId)
                .ExecuteDeleteAsync(cancellationToken);

            removed++;
        }

        return removed;
    }
}
