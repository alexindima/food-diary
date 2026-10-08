using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Modules.Hydration.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Hydration.Infrastructure.Tests;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class HydrationTypedAmountPersistenceTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TypedQuantity_RoundTripsThroughTheExistingIntegerColumnAsync() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"typed-hydration-{Guid.NewGuid():N}@example.com", "hash");
        var timestamp = new DateTime(2026, 10, 8, 9, 30, 0, DateTimeKind.Utc);
        var entry = HydrationEntry.CreateWithAmount(user.Id, timestamp, HydrationAmount.FromMilliliters(250));
        context.AddRange(user, entry);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        HydrationEntry loaded = await context.HydrationEntries.SingleAsync(value => value.Id == entry.Id);
        Assert.Equal(250, loaded.AmountMl);
        loaded.UpdateDetails(HydrationAmount.FromMilliliters(500));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        HydrationEntry reloaded = await context.HydrationEntries.SingleAsync(value => value.Id == entry.Id);

        Assert.Multiple(() => {
            Assert.Equal(500, reloaded.AmountMl);
            Assert.Equal(timestamp, reloaded.Timestamp);
            Assert.Equal(DateTimeKind.Utc, reloaded.Timestamp.Kind);
            Assert.NotNull(reloaded.ModifiedOnUtc);
        });
    }
}
