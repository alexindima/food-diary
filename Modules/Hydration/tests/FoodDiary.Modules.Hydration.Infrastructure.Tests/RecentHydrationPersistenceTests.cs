using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Hydration.Infrastructure.Tests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class RecentHydrationPersistenceTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task RecentEntries_ApplyOwnerInclusiveBoundsDescendingOrderAndLimitWithoutTracking() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var owner = User.Create($"recent-water-{Guid.NewGuid():N}@example.test", "hash");
        var other = User.Create($"other-water-{Guid.NewGuid():N}@example.test", "hash");
        DateTime start = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        DateTime end = start.AddHours(1);
        var oldest = HydrationEntry.Create(owner.Id, start, 100);
        var newest = HydrationEntry.Create(owner.Id, end, 200);
        context.Users.AddRange(owner, other);
        context.HydrationEntries.AddRange(oldest, newest,
            HydrationEntry.Create(owner.Id, start.AddTicks(-10), 300),
            HydrationEntry.Create(owner.Id, end.AddTicks(10), 400),
            HydrationEntry.Create(other.Id, end, 500));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new HydrationEntryRepository(context.HydrationEntries);

        IReadOnlyList<HydrationEntryReadModel> result = await repository.GetRecentReadModelsAsync(owner.Id, start, end, 10);
        IReadOnlyList<HydrationEntryReadModel> limited = await repository.GetRecentReadModelsAsync(owner.Id, start, end, 1);

        Assert.Multiple(
            () => Assert.Equal(new[] { newest.Id.Value, oldest.Id.Value }, result.Select(row => row.Id)),
            () => Assert.Equal(newest.Id.Value, Assert.Single(limited).Id),
            () => Assert.Equal(200, result[0].AmountMl),
            () => Assert.Equal(end, result[0].Timestamp),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }
}
