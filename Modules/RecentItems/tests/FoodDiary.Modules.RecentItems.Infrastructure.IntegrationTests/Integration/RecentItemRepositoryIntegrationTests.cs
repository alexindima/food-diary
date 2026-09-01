using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecentItemRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task RegisterUsageAsync_WhenCallsRace_UpsertsOnceAndPreservesNewestTimestamp() {
        var baseline = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
        var user = User.Create("recent-concurrency@example.com", "hash");
        var existingProductId = new ProductId(Guid.NewGuid());
        var newProductId = new ProductId(Guid.NewGuid());

        await using FoodDiaryDbContext seedContext = await databaseFixture.CreateDbContextAsync();
        seedContext.Users.Add(user);
        seedContext.RecentItems.Add(RecentItem.Create(
            user.Id,
            RecentItemType.Product,
            existingProductId.Value,
            baseline));
        await seedContext.SaveChangesAsync();

        await using FoodDiaryDbContext firstContext = CreateVerificationContext(seedContext);
        await using FoodDiaryDbContext secondContext = CreateVerificationContext(seedContext);
        var firstRepository = new RecentItemRepository(firstContext, new FixedDateTimeProvider(baseline.AddMinutes(1)));
        var secondRepository = new RecentItemRepository(secondContext, new FixedDateTimeProvider(baseline.AddMinutes(2)));

        await Task.WhenAll(
            firstRepository.RegisterUsageAsync(user.Id, [existingProductId, newProductId], [], CancellationToken.None),
            secondRepository.RegisterUsageAsync(user.Id, [existingProductId, newProductId], [], CancellationToken.None));

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(seedContext);
        List<RecentItem> storedItems = await verificationContext.RecentItems
            .AsNoTracking()
            .Where(item => item.UserId == user.Id && item.ItemType == RecentItemType.Product)
            .OrderBy(item => item.ItemId)
            .ToListAsync();

        RecentItem existingItem = Assert.Single(storedItems, item => item.ItemId == existingProductId.Value);
        RecentItem newItem = Assert.Single(storedItems, item => item.ItemId == newProductId.Value);
        Assert.Multiple(
            () => Assert.Equal(3, existingItem.UsageCount),
            () => Assert.Equal(2, newItem.UsageCount),
            () => Assert.Equal(baseline.AddMinutes(2), existingItem.LastUsedAtUtc),
            () => Assert.Equal(baseline.AddMinutes(2), newItem.LastUsedAtUtc));
    }

    [RequiresDockerFact]
    public async Task RegisterUsageAsync_WhenRecentProductsAtCapacity_KeepsNewestHundredIncludingNewItem() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recent@example.com", "hash");
        context.Users.Add(user);

        var baseline = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
        Guid[] existingProductIds = [.. Enumerable.Range(0, 100).Select(_ => Guid.NewGuid())];

        for (int i = 0; i < existingProductIds.Length; i++) {
            context.RecentItems.Add(RecentItem.Create(
                user.Id,
                RecentItemType.Product,
                existingProductIds[i],
                baseline.AddMinutes(-(i + 1))));
        }

        await context.SaveChangesAsync();

        var newProductId = new ProductId(Guid.NewGuid());
        var repository = new RecentItemRepository(context, new FixedDateTimeProvider(baseline));

        await repository.RegisterUsageAsync(
            user.Id,
            [newProductId],
            [],
            CancellationToken.None);
        await context.SaveChangesAsync();

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(context);

        List<RecentItem> storedItems = await verificationContext.RecentItems
            .AsNoTracking()
            .Where(x => x.UserId == user.Id && x.ItemType == RecentItemType.Product)
            .OrderByDescending(x => x.LastUsedAtUtc)
            .ThenByDescending(x => x.CreatedOnUtc)
            .ToListAsync();

        Assert.Equal(100, storedItems.Count);
        Assert.Equal(newProductId.Value, storedItems[0].ItemId);
        Assert.DoesNotContain(storedItems, x => x.ItemId == existingProductIds[^1]);
        Assert.Contains(storedItems, x => x.ItemId == existingProductIds[0]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private static FoodDiaryDbContext CreateVerificationContext(FoodDiaryDbContext sourceContext) {
        string connectionString = sourceContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Source context does not have a connection string.");

        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString).ConnectionString)
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
