using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class HydrationEntryRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task SaveChangesAsync_WithDuplicateUserTimestamp_PersistsBothEntries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"hydration-duplicate-{Guid.NewGuid():N}@example.com", "hash");
        DateTime timestampUtc = DateTime.UtcNow;
        context.Users.Add(user);
        context.HydrationEntries.AddRange(
            HydrationEntry.Create(user.Id, timestampUtc, 250),
            HydrationEntry.Create(user.Id, timestampUtc, 500));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.HydrationEntries.CountAsync(entry => entry.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task SaveChangesAsync_WithInvalidAmount_RejectsDatabaseWrite() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"hydration-check-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "HydrationEntries" ("Id", "UserId", "Timestamp", "AmountMl", "CreatedOnUtc")
            VALUES ({Guid.NewGuid()}, {user.Id.Value}, {DateTime.UtcNow}, {0}, {DateTime.UtcNow})
            """));
    }

    [RequiresDockerFact]
    public async Task SaveChangesAsync_WithConcurrentHydrationUpdates_RejectsStaleWriter() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        Guid entryId;
        await using (FoodDiaryDbContext setupContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true)) {
            await setupContext.Database.MigrateAsync();
            var user = User.Create($"hydration-concurrency-{Guid.NewGuid():N}@example.com", "hash");
            var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
            entryId = entry.Id.Value;
            setupContext.Users.Add(user);
            setupContext.HydrationEntries.Add(entry);
            await setupContext.SaveChangesAsync();
        }

        await using FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(connectionString, enableRetries: true);
        var hydrationEntryId = new HydrationEntryId(entryId);
        HydrationEntry firstCopy = await firstContext.HydrationEntries.SingleAsync(entry => entry.Id == hydrationEntryId);
        HydrationEntry staleCopy = await secondContext.HydrationEntries.SingleAsync(entry => entry.Id == hydrationEntryId);
        firstCopy.Update(amountMl: 500);
        staleCopy.Update(amountMl: 750);

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }
}
