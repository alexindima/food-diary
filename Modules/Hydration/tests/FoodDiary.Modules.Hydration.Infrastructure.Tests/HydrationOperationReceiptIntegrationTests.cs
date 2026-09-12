using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class HydrationOperationReceiptIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ReceiptSurvivesEntryDeletionAndIsPurgedWithAccount() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"water-receipt-{Guid.NewGuid():N}@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var receipt = HydrationOperationReceipt.Create(Guid.NewGuid(), entry);
        var repository = new HydrationOperationReceiptRepository(context.Set<HydrationOperationReceipt>());
        context.AddRange(user, entry);
        await repository.AddAsync(receipt);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Null(await repository.FindAsync(new UserId(Guid.NewGuid()), receipt.OperationId));
        await context.HydrationEntries.Where(candidate => candidate.Id == entry.Id).ExecuteDeleteAsync();
        HydrationOperationReceipt? preserved = await repository.FindAsync(user.Id, receipt.OperationId);
        Assert.NotNull(preserved);
        Assert.True(preserved.Matches(user.Id, 250, entry.Timestamp));
        await context.Users.Where(candidate => candidate.Id == user.Id).ExecuteDeleteAsync();
        Assert.False(await context.Set<HydrationOperationReceipt>().AnyAsync(candidate => candidate.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task DuplicateOperationFailsAndRollsBackItsNewEntry() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"water-duplicate-{Guid.NewGuid():N}@example.com", "hash");
        var entry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 250);
        var operationId = Guid.NewGuid();
        context.AddRange(user, entry, HydrationOperationReceipt.Create(operationId, entry));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var duplicateEntry = HydrationEntry.Create(user.Id, DateTime.UtcNow, 500);
        context.AddRange(duplicateEntry, HydrationOperationReceipt.Create(operationId, duplicateEntry));

        DbUpdateException error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        PostgresException postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        context.ChangeTracker.Clear();
        Assert.False(await context.HydrationEntries.AnyAsync(candidate => candidate.Id == duplicateEntry.Id));
        Assert.Equal(250, await context.HydrationEntries.Where(candidate => candidate.UserId == user.Id).SumAsync(candidate => candidate.AmountMl));
    }
}
