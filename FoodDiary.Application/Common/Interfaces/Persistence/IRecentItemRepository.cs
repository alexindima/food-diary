using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IRecentItemRepository
{
    Task RegisterUsageAsync(
        UserId userId,
        IReadOnlyCollection<ProductId> productIds,
        IReadOnlyCollection<RecipeId> recipeIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
        UserId userId,
        int limit,
        CancellationToken cancellationToken = default);
}

public sealed record RecentProductUsage(ProductId ProductId, int UsageCount, DateTime LastUsedAtUtc);

public sealed record RecentRecipeUsage(RecipeId RecipeId, int UsageCount, DateTime LastUsedAtUtc);
