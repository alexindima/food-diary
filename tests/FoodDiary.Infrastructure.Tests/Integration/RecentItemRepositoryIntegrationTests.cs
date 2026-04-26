using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Recents;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.RecentItems;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class RecentItemRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task RegisterUsageAsync_WhenRecentProductsAtCapacity_KeepsNewestHundredIncludingNewItem() {
        await using var context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recent@example.com", "hash");
        context.Users.Add(user);

        var baseline = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
        var existingProductIds = Enumerable.Range(0, 100)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        for (var i = 0; i < existingProductIds.Length; i++) {
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

        await using var verificationContext = CreateVerificationContext(context);

        var storedItems = await verificationContext.RecentItems
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

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider {
        public DateTime UtcNow { get; } = utcNow;
    }

    private static FoodDiaryDbContext CreateVerificationContext(FoodDiaryDbContext sourceContext) {
        var connectionString = sourceContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Source context does not have a connection string.");

        var options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString).ConnectionString)
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
