using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class HydrationIntervalReadServiceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Read_UsesHalfOpenInstantsAndOwnerWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"water-interval-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"water-other-{Guid.NewGuid():N}@example.com", "hash");
        DateTime start = new(2026, 3, 8, 5, 0, 0, DateTimeKind.Utc);
        DateTime end = start.AddHours(23);
        context.AddRange(owner, other,
            HydrationEntry.Create(owner.Id, start.AddTicks(-10), 100),
            HydrationEntry.Create(owner.Id, start, 250),
            HydrationEntry.Create(owner.Id, end.AddTicks(-10), 500),
            HydrationEntry.Create(owner.Id, end, 1000),
            HydrationEntry.Create(other.Id, start, 2000));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new HydrationIntervalReadService(context.HydrationEntries);

        long total = await reader.GetTotalAsync(owner.Id, start, end);
        long empty = await reader.GetTotalAsync(owner.Id, end.AddDays(1), end.AddDays(2));
        long zeroDuration = await reader.GetTotalAsync(owner.Id, start, start);

        Assert.Multiple(
            () => Assert.Equal(750, total),
            () => Assert.Equal(0, empty),
            () => Assert.Equal(0, zeroDuration),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }
}
