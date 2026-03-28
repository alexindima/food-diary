using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class MigrationSafetyIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string InitialMigration = "20251108210736_InitialCreate";

    [RequiresDockerFact]
    public async Task CleanDatabase_MigrateToLatest_AppliesFullMigrationChain() {
        var connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using var context = databaseFixture.CreateDbContext(connectionString);

        await context.Database.MigrateAsync();

        var allMigrations = context.Database.GetMigrations().ToList();
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Equal(allMigrations, appliedMigrations);
        Assert.Empty(pendingMigrations);
        Assert.True(await context.Database.CanConnectAsync());
    }

    [RequiresDockerFact]
    public async Task DatabaseAtInitialCreate_CanUpgradeToLatest() {
        var connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();

        await using (var initialContext = databaseFixture.CreateDbContext(connectionString)) {
            var migrator = initialContext.GetService<IMigrator>();
            await migrator.MigrateAsync(InitialMigration);

            var appliedMigrations = await initialContext.Database.GetAppliedMigrationsAsync();
            Assert.Equal([InitialMigration], appliedMigrations);
        }

        await using var upgradedContext = databaseFixture.CreateDbContext(connectionString);
        await upgradedContext.Database.MigrateAsync();

        var allMigrations = upgradedContext.Database.GetMigrations().ToList();
        var appliedAfterUpgrade = (await upgradedContext.Database.GetAppliedMigrationsAsync()).ToList();
        var pendingAfterUpgrade = (await upgradedContext.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Equal(allMigrations, appliedAfterUpgrade);
        Assert.Empty(pendingAfterUpgrade);
    }
}
