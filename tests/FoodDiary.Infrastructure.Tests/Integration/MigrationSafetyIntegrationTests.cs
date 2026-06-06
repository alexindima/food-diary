using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MigrationSafetyIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string InitialMigration = "20251108210736_InitialCreate";

    [Fact]
    public void MigrationTypes_AreExcludedFromCodeCoverage() {
        string?[] migrationTypesMissingAttribute = typeof(global::FoodDiary.Infrastructure.Persistence.FoodDiaryDbContext).Assembly
            .GetTypes()
            .Where(static type => string.Equals(type.Namespace, "FoodDiary.Infrastructure.Migrations", StringComparison.Ordinal))
            .Where(static type => type.IsNested is false)
            .Where(static type => typeof(Migration).IsAssignableFrom(type) || typeof(ModelSnapshot).IsAssignableFrom(type))
            .Where(static type => type.GetCustomAttributes(typeof(ExcludeFromCodeCoverageAttribute), inherit: false).Length == 0)
            .Select(static type => type.FullName)
            .OrderBy(static typeName => typeName, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(migrationTypesMissingAttribute);
    }

    [RequiresDockerFact]
    public async Task CleanDatabase_MigrateToLatest_AppliesFullMigrationChain() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);

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
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();

        await using (FoodDiaryDbContext initialContext = databaseFixture.CreateDbContext(connectionString)) {
            IMigrator migrator = initialContext.GetService<IMigrator>();
            await migrator.MigrateAsync(InitialMigration);

            IEnumerable<string> appliedMigrations = await initialContext.Database.GetAppliedMigrationsAsync();
            Assert.Equal([InitialMigration], appliedMigrations);
        }

        await using FoodDiaryDbContext upgradedContext = databaseFixture.CreateDbContext(connectionString);
        await upgradedContext.Database.MigrateAsync();

        var allMigrations = upgradedContext.Database.GetMigrations().ToList();
        var appliedAfterUpgrade = (await upgradedContext.Database.GetAppliedMigrationsAsync()).ToList();
        var pendingAfterUpgrade = (await upgradedContext.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Equal(allMigrations, appliedAfterUpgrade);
        Assert.Empty(pendingAfterUpgrade);
    }
}
