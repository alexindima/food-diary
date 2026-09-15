using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecentItems.Domain.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecentItems.Infrastructure.Persistence;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.RecentItems.Domain.Entities.Recents;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.RecentItems.Infrastructure.Persistence.RecentItems;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.RecentItems.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class RecentItemRepositoryTests {
    [Fact]
    public async Task RegisterUsageAsync_WithInMemoryProvider_AddsAndUpdatesDistinctItems() {
        await using RecentItemsDbContext context = CreateContext();
        var usedAtUtc = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc);
        var timeProvider = new MutableTimeProvider(usedAtUtc);
        var repository = new RecentItemRepository(context, static () => null, timeProvider);
        var userId = UserId.New();
        var productId = ProductId.New();
        var recipeId = RecipeId.New();

        await repository.RegisterUsageAsync(
            userId,
            [productId, productId],
            [recipeId, recipeId],
            CancellationToken.None);
        await context.SaveChangesAsync();

        timeProvider.UtcNow = usedAtUtc.AddMinutes(5);
        await repository.RegisterUsageAsync(userId, [productId], [], CancellationToken.None);
        await context.SaveChangesAsync();

        RecentProductUsage product = Assert.Single(
            await repository.GetRecentProductsAsync(userId, limit: 10, CancellationToken.None));
        RecentRecipeUsage recipe = Assert.Single(
            await repository.GetRecentRecipesAsync(userId, limit: 10, CancellationToken.None));
        Assert.Multiple(
            () => Assert.Equal(2, product.UsageCount),
            () => Assert.Equal(timeProvider.UtcNow, product.LastUsedAtUtc),
            () => Assert.Equal(1, recipe.UsageCount),
            () => Assert.Equal(usedAtUtc, recipe.LastUsedAtUtc));
    }

    [Fact]
    public async Task RegisterUsageAsync_WithInMemoryProvider_TrimsOldestItems() {
        await using RecentItemsDbContext context = CreateContext();
        var usedAtUtc = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc);
        var timeProvider = new MutableTimeProvider(usedAtUtc.AddHours(1));
        var repository = new RecentItemRepository(context, static () => null, timeProvider);
        var userId = UserId.New();
        var oldestItemId = Guid.NewGuid();
        context.RecentItems.Add(RecentItem.Create(userId, RecentItemType.Product, oldestItemId, usedAtUtc));
        for (int index = 1; index < 100; index++) {
            context.RecentItems.Add(RecentItem.Create(
                userId,
                RecentItemType.Product,
                Guid.NewGuid(),
                usedAtUtc.AddMinutes(index)));
        }

        await context.SaveChangesAsync();
        var newestProductId = ProductId.New();

        await repository.RegisterUsageAsync(userId, [newestProductId], [], CancellationToken.None);
        await context.SaveChangesAsync();

        List<RecentItem> storedItems = await context.RecentItems
            .Where(item => item.UserId == userId && item.ItemType == RecentItemType.Product)
            .ToListAsync();
        Assert.Multiple(
            () => Assert.Equal(100, storedItems.Count),
            () => Assert.Contains(storedItems, item => item.ItemId == newestProductId.Value),
            () => Assert.DoesNotContain(storedItems, item => item.ItemId == oldestItemId));
    }

    [Fact]
    public async Task RegisterUsageAsync_WithNoItems_DoesNotTrackChanges() {
        await using RecentItemsDbContext context = CreateContext();
        var repository = new RecentItemRepository(context, static () => null, TimeProvider.System);

        await repository.RegisterUsageAsync(UserId.New(), [], [], CancellationToken.None);

        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task RegisterUsageAsync_WithEmptyUserId_ThrowsBeforePersistence() {
        await using RecentItemsDbContext context = CreateContext();
        var repository = new RecentItemRepository(context, static () => null, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.RegisterUsageAsync(UserId.Empty, [], [], CancellationToken.None));
    }

    [Fact]
    public async Task RegisterUsageAsync_WithEmptyProductId_ThrowsBeforePersistence() {
        await using RecentItemsDbContext context = CreateContext();
        var repository = new RecentItemRepository(context, static () => null, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.RegisterUsageAsync(UserId.New(), [ProductId.Empty], [], CancellationToken.None));
    }

    [Fact]
    public async Task RegisterUsageAsync_WithEmptyRecipeId_ThrowsBeforePersistence() {
        await using RecentItemsDbContext context = CreateContext();
        var repository = new RecentItemRepository(context, static () => null, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.RegisterUsageAsync(UserId.New(), [], [RecipeId.Empty], CancellationToken.None));
    }

    private static RecentItemsDbContext CreateContext() {
        DbContextOptions<RecentItemsDbContext> options = new DbContextOptionsBuilder<RecentItemsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new RecentItemsDbContext(options);
    }

    [ExcludeFromCodeCoverage]
    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider {
        public DateTime UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => new(UtcNow);
    }
}
