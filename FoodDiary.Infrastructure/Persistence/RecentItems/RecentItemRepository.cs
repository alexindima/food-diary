using FoodDiary.Application.Abstractions.RecentItems.Common;
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
            await UpsertItemsAsync(userId, RecentItemType.Product, distinctProductIds, now, cancellationToken).ConfigureAwait(false);
        }

        if (distinctRecipeIds.Count > 0) {
            await UpsertItemsAsync(userId, RecentItemType.Recipe, distinctRecipeIds, now, cancellationToken).ConfigureAwait(false);
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

    private async Task UpsertItemsAsync(
        UserId userId,
        RecentItemType itemType,
        IReadOnlyCollection<Guid> itemIds,
        DateTime usedAtUtc,
        CancellationToken cancellationToken) {
        Guid[] ids = [.. itemIds];
        Guid[] recentItemIds = [.. ids.Select(_ => RecentItemId.New().Value)];
        string itemTypeValue = itemType.ToString();

        await context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO "RecentItems" (
                "Id", "UserId", "ItemType", "ItemId", "LastUsedAtUtc", "UsageCount", "CreatedOnUtc", "ModifiedOnUtc")
            SELECT input."Id", {{userId.Value}}, {{itemTypeValue}}, input."ItemId", {{usedAtUtc}}, 1, {{usedAtUtc}}, NULL
            FROM unnest({{recentItemIds}}, {{ids}}) AS input("Id", "ItemId")
            ON CONFLICT ("UserId", "ItemType", "ItemId") DO UPDATE SET
                "LastUsedAtUtc" = GREATEST("RecentItems"."LastUsedAtUtc", EXCLUDED."LastUsedAtUtc"),
                "UsageCount" = LEAST("RecentItems"."UsageCount"::bigint + 1, {{int.MaxValue}})::integer,
                "ModifiedOnUtc" = GREATEST(
                    COALESCE("RecentItems"."ModifiedOnUtc", "RecentItems"."CreatedOnUtc"),
                    EXCLUDED."LastUsedAtUtc")
            """, cancellationToken).ConfigureAwait(false);
    }

    private async Task TrimOverflowAsync(
        UserId userId,
        RecentItemType itemType,
        CancellationToken cancellationToken) {
        string itemTypeValue = itemType.ToString();
        await context.Database.ExecuteSqlInterpolatedAsync($$"""
            DELETE FROM "RecentItems"
            WHERE "Id" IN (
                SELECT "Id"
                FROM "RecentItems"
                WHERE "UserId" = {{userId.Value}} AND "ItemType" = {{itemTypeValue}}
                ORDER BY "LastUsedAtUtc" DESC, "CreatedOnUtc" DESC
                OFFSET {{MaxStoredPerType}})
            """, cancellationToken).ConfigureAwait(false);
    }
}
