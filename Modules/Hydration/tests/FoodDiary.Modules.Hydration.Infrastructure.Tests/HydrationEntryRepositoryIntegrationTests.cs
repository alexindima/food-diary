using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class HydrationEntryRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void NavigationFreeHydration_DoesNotRequireARelationalMigration() {
        using FoodDiaryDbContext context = databaseFixture.CreateDbContext("Host=localhost;Database=model_check;Username=model_check");
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task NarrowRepository_UsesTheCallersTransactionAndPreservesUserCascade() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"hydration-scope-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var repository = new HydrationEntryRepository(context.HydrationEntries);
        var rolledBack = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);

        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
            await repository.AddAsync(rolledBack);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        context.ChangeTracker.Clear();
        Assert.False(await context.HydrationEntries.AnyAsync(entry => entry.Id == rolledBack.Id));

        var persisted = HydrationEntry.Create(user.Id, DateTime.UtcNow, 500);
        await repository.AddAsync(persisted);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.Single(await repository.GetByDateAsync(user.Id, persisted.Timestamp));
        Assert.Empty(context.ChangeTracker.Entries());

        await context.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync();
        Assert.False(await context.HydrationEntries.AnyAsync(entry => entry.UserId == user.Id));
    }

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
