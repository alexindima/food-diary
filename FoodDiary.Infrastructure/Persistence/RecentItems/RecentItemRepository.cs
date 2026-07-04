using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.RecentItems;

public sealed class RecentItemRepository(FoodDiaryDbContext context, TimeProvider dateTimeProvider) : IRecentItemRepository {
    private const int MaxStoredPerType = 100;

    public async Task RegisterUsageAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default) {
        var distinctProductIds = productIds
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        var distinctRecipeIds = recipeIds
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (distinctProductIds.Count == 0 && distinctRecipeIds.Count == 0) {
            return;
        }

        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;

        if (distinctProductIds.Count > 0) {
            await TouchItemsAsync(userId, RecentItemType.Product, distinctProductIds, now, cancellationToken).ConfigureAwait(false);
        }

        if (distinctRecipeIds.Count > 0) {
            await TouchItemsAsync(userId, RecentItemType.Recipe, distinctRecipeIds, now, cancellationToken).ConfigureAwait(false);
        }

        if (distinctProductIds.Count > 0) {
            await TrimOverflowAsync(userId, RecentItemType.Product, cancellationToken).ConfigureAwait(false);
        }

        if (distinctRecipeIds.Count > 0) {
            await TrimOverflowAsync(userId, RecentItemType.Recipe, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default) {
        int sanitizedLimit = Math.Clamp(limit, 1, 100);

        return await context.RecentItems
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ItemType == RecentItemType.Product)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .Take(sanitizedLimit)
            .Select(x => new RecentProductUsage(new ProductId(x.ItemId), x.UsageCount, x.LastUsedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default) {
        int sanitizedLimit = Math.Clamp(limit, 1, 100);

        return await context.RecentItems
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ItemType == RecentItemType.Recipe)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .Take(sanitizedLimit)
            .Select(x => new RecentRecipeUsage(new RecipeId(x.ItemId), x.UsageCount, x.LastUsedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task TouchItemsAsync(
        UserId userId,
        RecentItemType itemType,
        IReadOnlyCollection<Guid> itemIds,
        DateTime usedAtUtc,
        CancellationToken cancellationToken) {
        List<RecentItem> existingItems = await context.RecentItems
            .Where(x => x.UserId == userId && x.ItemType == itemType && itemIds.Contains(x.ItemId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var existingByItemId = existingItems.ToDictionary(x => x.ItemId);

        foreach (Guid itemId in itemIds) {
            if (existingByItemId.TryGetValue(itemId, out RecentItem? existing)) {
                existing.Touch(usedAtUtc);
                continue;
            }

            context.RecentItems.Add(RecentItem.Create(userId, itemType, itemId, usedAtUtc));
        }
    }

    private async Task TrimOverflowAsync(
        UserId userId,
        RecentItemType itemType,
        CancellationToken cancellationToken) {
        List<RecentItem> storedItems = await context.RecentItems
            .Where(x => x.UserId == userId && x.ItemType == itemType)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        RecentItem[] addedItems = [.. context.ChangeTracker
            .Entries<RecentItem>()
            .Where(entry =>
                entry.State == EntityState.Added &&
                entry.Entity.UserId == userId &&
                entry.Entity.ItemType == itemType)
            .Select(entry => entry.Entity)];

        RecentItem[] overflowItems = [.. storedItems
            .Concat(addedItems)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .ThenByDescending(x => x.CreatedOnUtc)
            .Skip(MaxStoredPerType)];

        if (overflowItems.Length > 0) {
            context.RecentItems.RemoveRange(overflowItems);
        }
    }
}
