using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MigrationSafetyIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string InitialMigration = "20251108210736_InitialCreate";
    private const string BeforeFastingProtocolRenameMigration = "20260810221016_AddWeeklyGoals";

    [Fact]
    public void MigrationTypes_AreExcludedFromCodeCoverage() {
        string?[] migrationTypesMissingAttribute = [.. typeof(global::FoodDiary.Infrastructure.Persistence.FoodDiaryDbContext).Assembly
            .GetTypes()
            .Where(static type => string.Equals(type.Namespace, "FoodDiary.Infrastructure.Migrations", StringComparison.Ordinal))
            .Where(static type => !type.IsNested)
            .Where(static type => typeof(Migration).IsAssignableFrom(type) || typeof(ModelSnapshot).IsAssignableFrom(type))
            .Where(static type => type.GetCustomAttributes(typeof(ExcludeFromCodeCoverageAttribute), inherit: false).Length == 0)
            .Select(static type => type.FullName)
            .Order(StringComparer.Ordinal)];

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
            Assert.Equal([InitialMigration], appliedMigrations, StringComparer.Ordinal);
        }

        await using FoodDiaryDbContext upgradedContext = databaseFixture.CreateDbContext(connectionString);
        await upgradedContext.Database.MigrateAsync();

        var allMigrations = upgradedContext.Database.GetMigrations().ToList();
        var appliedAfterUpgrade = (await upgradedContext.Database.GetAppliedMigrationsAsync()).ToList();
        var pendingAfterUpgrade = (await upgradedContext.Database.GetPendingMigrationsAsync()).ToList();

        Assert.Equal(allMigrations, appliedAfterUpgrade);
        Assert.Empty(pendingAfterUpgrade);
    }

    [RequiresDockerFact]
    public async Task DatabaseWithLegacyFastingProtocols_UpgradesToCanonicalValues() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        FastingProtocol[] protocols = [
            FastingProtocol.Fast16Eat8,
            FastingProtocol.Fast18Eat6,
            FastingProtocol.Fast20Eat4,
            FastingProtocol.Fast24,
            FastingProtocol.Fast36,
            FastingProtocol.Fast72,
        ];

        await using (FoodDiaryDbContext legacyContext = databaseFixture.CreateDbContext(connectionString)) {
            IMigrator migrator = legacyContext.GetService<IMigrator>();
            await migrator.MigrateAsync(BeforeFastingProtocolRenameMigration);

            var user = User.Create($"fasting-migration-{Guid.NewGuid():N}@example.com", "hash");
            legacyContext.Users.Add(user);
            legacyContext.FastingSessions.AddRange(protocols.Select(protocol =>
                FastingSession.Create(user.Id, protocol, FastingSession.GetDefaultDuration(protocol), DateTime.UtcNow)));
            legacyContext.FastingPlans.Add(FastingPlan.CreateExtended(
                user.Id,
                FastingProtocol.Fast24,
                24,
                DateTime.UtcNow));
            legacyContext.FastingTelemetryEvents.Add(FastingTelemetryEvent.Create(
                "migration-test",
                DateTime.UtcNow,
                protocol: FastingProtocol.Fast24.ToString()));
            await legacyContext.SaveChangesAsync();

            await legacyContext.Database.ExecuteSqlRawAsync("""
                UPDATE "FastingSessions"
                SET "Protocol" = CASE "Protocol"
                    WHEN 'Fast16Eat8' THEN 'F16_8'
                    WHEN 'Fast18Eat6' THEN 'F18_6'
                    WHEN 'Fast20Eat4' THEN 'F20_4'
                    WHEN 'Fast24' THEN 'F24_0'
                    WHEN 'Fast36' THEN 'F36_0'
                    WHEN 'Fast72' THEN 'F72_0'
                    ELSE "Protocol"
                END;

                UPDATE "FastingPlans" SET "Protocol" = 'F24_0' WHERE "Protocol" = 'Fast24';
                UPDATE "FastingTelemetryEvents" SET "Protocol" = 'F24_0' WHERE "Protocol" = 'Fast24';
                """);
        }

        await using FoodDiaryDbContext upgradedContext = databaseFixture.CreateDbContext(connectionString);
        await upgradedContext.Database.MigrateAsync();

        FastingProtocol[] storedSessionProtocols = await upgradedContext.FastingSessions
            .Select(session => session.Protocol)
            .ToArrayAsync();
        Array.Sort(storedSessionProtocols);
        FastingProtocol? storedPlanProtocol = await upgradedContext.FastingPlans
            .Select(plan => plan.Protocol)
            .SingleAsync();
        string? storedTelemetryProtocol = await upgradedContext.FastingTelemetryEvents
            .Select(telemetryEvent => telemetryEvent.Protocol)
            .SingleAsync();

        Assert.Multiple(
            () => Assert.Equal(protocols, storedSessionProtocols),
            () => Assert.Equal(FastingProtocol.Fast24, storedPlanProtocol),
            () => Assert.Equal(nameof(FastingProtocol.Fast24), storedTelemetryProtocol));
    }
}
