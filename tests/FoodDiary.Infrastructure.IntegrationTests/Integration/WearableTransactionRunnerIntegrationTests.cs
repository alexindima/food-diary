using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Wearables;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class WearableTransactionRunnerIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task ExecuteSerializedAsync_DoesNotHoldTransactionWhileOperationRuns() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var runner = new EfWearableTransactionRunner(context);

        bool result = await runner.ExecuteSerializedAsync(
            $"wearable-transaction-scope:{Guid.NewGuid():N}",
            _ => {
                Assert.Null(context.Database.CurrentTransaction);
                return Task.FromResult(true);
            },
            CancellationToken.None);

        Assert.True(result);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [RequiresDockerFact]
    public async Task ExecuteSerializedAsync_WhenConnectionCreationRaces_CreatesSingleConnection() {
        var user = User.Create("wearable-race@example.com", "hash");
        await using FoodDiaryDbContext seedContext = await databaseFixture.CreateDbContextAsync();
        seedContext.Users.Add(user);
        await seedContext.SaveChangesAsync();

        await using FoodDiaryDbContext firstContext = CreateContext(seedContext);
        await using FoodDiaryDbContext secondContext = CreateContext(seedContext);
        var firstRunner = new EfWearableTransactionRunner(firstContext);
        var secondRunner = new EfWearableTransactionRunner(secondContext);
        string serializationKey = $"wearable-connect:{user.Id.Value:N}:{WearableProvider.Fitbit}";

        await Task.WhenAll(
            CreateConnectionIfMissingAsync(firstRunner, firstContext, serializationKey, user.Id, "first"),
            CreateConnectionIfMissingAsync(secondRunner, secondContext, serializationKey, user.Id, "second"));

        await using FoodDiaryDbContext verificationContext = CreateContext(seedContext);
        int connectionCount = await verificationContext.WearableConnections
            .CountAsync(connection => connection.UserId == user.Id && connection.Provider == WearableProvider.Fitbit);

        Assert.Equal(1, connectionCount);
    }

    private static Task<bool> CreateConnectionIfMissingAsync(
        EfWearableTransactionRunner runner,
        FoodDiaryDbContext context,
        string serializationKey,
        FoodDiary.Domain.ValueObjects.Ids.UserId userId,
        string externalUserId) =>
        runner.ExecuteSerializedAsync(
            serializationKey,
            async cancellationToken => {
                bool exists = await context.WearableConnections.AnyAsync(
                    connection => connection.UserId == userId && connection.Provider == WearableProvider.Fitbit,
                    cancellationToken);
                if (exists) {
                    return false;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);
                context.WearableConnections.Add(WearableConnection.Create(
                    userId,
                    WearableProvider.Fitbit,
                    externalUserId,
                    "access-token",
                    refreshToken: null,
                    tokenExpiresAtUtc: null));
                return true;
            },
            CancellationToken.None);

    private static FoodDiaryDbContext CreateContext(FoodDiaryDbContext sourceContext) {
        string connectionString = sourceContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Source context does not have a connection string.");
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString).ConnectionString)
            .Options;
        return new FoodDiaryDbContext(options);
    }
}
