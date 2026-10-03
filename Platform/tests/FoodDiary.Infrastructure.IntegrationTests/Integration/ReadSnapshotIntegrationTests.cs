using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Application.Contracts.Common.Abstractions.Events;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ReadSnapshotIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task LateEnlistedOwners_ReadTheSameCommittedSnapshot() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("read-snapshot@example.com", "hash");
        user.UpdatePersonalInfo(firstName: "Before");
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        await using ServiceProvider provider = CreateProvider(seed.Database.GetConnectionString()!);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IReadSnapshotExecutor executor = scope.ServiceProvider.GetRequiredService<IReadSnapshotExecutor>();
        await executor.ExecuteAsync(new ReadSnapshotBudget(TimeSpan.FromSeconds(10), 5), async token => {
            UsersDbContext users = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            Assert.Equal("Before", await users.Users.Select(account => account.FirstName).SingleAsync(token));
            await seed.Users.ExecuteUpdateAsync(update => update.SetProperty(account => account.FirstName, "After"), token);
            Assert.Equal("Before", await users.Users.Select(account => account.FirstName).SingleAsync(token));
            return true;
        });
        Assert.Equal("After", await seed.Users.AsNoTracking().Select(account => account.FirstName).SingleAsync());
    }

    [RequiresDockerFact]
    public async Task QueryLimit_StopsFanoutAndReleasesTheTransaction() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(seed.Database.GetConnectionString()!);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IReadSnapshotExecutor executor = scope.ServiceProvider.GetRequiredService<IReadSnapshotExecutor>();
        SharedPersistenceDbContext root = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(new ReadSnapshotBudget(TimeSpan.FromSeconds(10), 2), async token => {
            for (int index = 0; index < 3; index++) {
                await root.Database.SqlQuery<int>($"SELECT 1 AS \"Value\"").SingleAsync(token);
            }
            return true;
        }));
        Assert.Null(root.Database.CurrentTransaction);
        Assert.Equal(1, await root.Database.SqlQuery<int>($"SELECT 1 AS \"Value\"").SingleAsync());
    }

    [RequiresDockerFact]
    public async Task Snapshot_IsReadOnlyAndHasARealDatabaseDeadline() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(seed.Database.GetConnectionString()!);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IReadSnapshotExecutor executor = scope.ServiceProvider.GetRequiredService<IReadSnapshotExecutor>();
        SharedPersistenceDbContext root = scope.ServiceProvider.GetRequiredService<SharedPersistenceDbContext>();
        PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => executor.ExecuteAsync(
            new ReadSnapshotBudget(TimeSpan.FromSeconds(10), 2), token => root.Database.ExecuteSqlRawAsync("DELETE FROM \"AtomicCommandReceipts\"", token)));
        Assert.Equal(PostgresErrorCodes.ReadOnlySqlTransaction, error.SqlState);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executor.ExecuteAsync(new ReadSnapshotBudget(TimeSpan.FromMilliseconds(250), 2),
            token => root.Database.SqlQuery<int>($"SELECT 1 AS \"Value\" FROM pg_sleep(10)").SingleAsync(token)));
        Assert.Null(root.Database.CurrentTransaction);
    }

    private static ServiceProvider CreateProvider(string connectionString) {
        var services = new ServiceCollection();
        services.AddPersistenceRuntime(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:DefaultConnection"] = connectionString }).Build());
        services.AddUsersPersistence();
        services.Replace(ServiceDescriptor.Singleton(Substitute.For<IDomainEventPublisher>()));
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
