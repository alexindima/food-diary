using System.Data.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Wearables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class WearableRetryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task TransientSave_RetriesPersistenceWithoutRepeatingProviderOperation() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("wearable-save-retry@example.com", "hash");
        seed.Add(user);
        await seed.SaveChangesAsync();
        var fault = new TransientInsertFault();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString(), npgsql => npgsql.EnableRetryOnFailure(2, TimeSpan.Zero, errorCodesToAdd: null))
            .AddInterceptors(fault).Options;
        await using var context = new FoodDiaryDbContext(options);
        var runner = new EfWearableTransactionRunner(context);
        int providerCalls = 0;
        bool result = await runner.ExecuteSerializedAsync($"retry-test:{user.Id.Value:N}", _ => {
            providerCalls++;
            context.WearableSyncEntries.Add(WearableSyncEntry.Create(user.Id, WearableProvider.Fitbit,
                WearableDataType.Steps, new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc), 1234));
            return Task.FromResult(true);
        });
        WearableSyncEntry saved = await seed.WearableSyncEntries.AsNoTracking().SingleAsync();
        Assert.Multiple(
            () => Assert.True(result),
            () => Assert.Equal(1, providerCalls),
            () => Assert.Equal(2, fault.Attempts),
            () => Assert.Equal(1234, saved.Value),
            () => Assert.Null(context.Database.CurrentTransaction));
    }

    [ExcludeFromCodeCoverage]
    private sealed class TransientInsertFault : DbCommandInterceptor {
        public int Attempts { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) {
            if (command.CommandText.Contains("INSERT INTO \"WearableSyncEntries\"", StringComparison.Ordinal) && ++Attempts == 1) {
                throw new TimeoutException("Simulated transient save failure before execution");
            }
            return ValueTask.FromResult(result);
        }
    }
}
