using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class RecentItemRepository : IRecentItemRepository
{
    private const int MaxStoredPerType = 100;

    private readonly FoodDiaryDbContext _context;

    public RecentItemRepository(FoodDiaryDbContext context) => _context = context;

    public async Task RegisterUsageAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default)
    {
        var distinctProductIds = productIds
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        var distinctRecipeIds = recipeIds
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (distinctProductIds.Count == 0 && distinctRecipeIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (distinctProductIds.Count > 0)
        {
            await TouchItemsAsync(userId, RecentItemType.Product, distinctProductIds, now, cancellationToken);
            await TrimOverflowAsync(userId, RecentItemType.Product, cancellationToken);
        }

        if (distinctRecipeIds.Count > 0)
        {
            await TouchItemsAsync(userId, RecentItemType.Recipe, distinctRecipeIds, now, cancellationToken);
            await TrimOverflowAsync(userId, RecentItemType.Recipe, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sanitizedLimit = Math.Clamp(limit, 1, 100);

        return await _context.RecentItems
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ItemType == RecentItemType.Product)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .Take(sanitizedLimit)
            .Select(x => new RecentProductUsage(new ProductId(x.ItemId), x.UsageCount, x.LastUsedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sanitizedLimit = Math.Clamp(limit, 1, 100);

        return await _context.RecentItems
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ItemType == RecentItemType.Recipe)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .Take(sanitizedLimit)
            .Select(x => new RecentRecipeUsage(new RecipeId(x.ItemId), x.UsageCount, x.LastUsedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private async Task TouchItemsAsync(
        UserId userId,
        RecentItemType itemType,
        IReadOnlyCollection<Guid> itemIds,
        DateTime usedAtUtc,
        CancellationToken cancellationToken)
    {
        var existingItems = await _context.RecentItems
            .Where(x => x.UserId == userId && x.ItemType == itemType && itemIds.Contains(x.ItemId))
            .ToListAsync(cancellationToken);

        var existingByItemId = existingItems.ToDictionary(x => x.ItemId);

        foreach (var itemId in itemIds)
        {
            if (existingByItemId.TryGetValue(itemId, out var existing))
            {
                existing.Touch(usedAtUtc);
                continue;
            }

            _context.RecentItems.Add(RecentItem.Create(userId, itemType, itemId, usedAtUtc));
        }
    }

    private async Task TrimOverflowAsync(
        UserId userId,
        RecentItemType itemType,
        CancellationToken cancellationToken)
    {
        var overflowItems = await _context.RecentItems
            .Where(x => x.UserId == userId && x.ItemType == itemType)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .Skip(MaxStoredPerType)
            .ToListAsync(cancellationToken);

        if (overflowItems.Count > 0)
        {
            _context.RecentItems.RemoveRange(overflowItems);
        }
    }
}
