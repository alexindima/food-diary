using System.Data.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class MigrationSafetyIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private const string Baseline = "20261017130323_InitialBaseline";

    [RequiresDockerFact]
    public async Task CleanDatabase_MigrateToLatest_AppliesBaselineAndReferenceData() {
        await using FoodDiaryDbContext context = await CreateBaselineDatabaseAsync();
        Assert.Equal([Baseline], await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(context.Database.GetMigrations().Skip(1), await context.Database.GetPendingMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(5, await context.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM "Roles"
            """).SingleAsync());
        Assert.Equal(10, await context.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM "DailyAdvices"
            """).SingleAsync());
        Assert.Equal(10, await context.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM "EmailTemplates"
            """).SingleAsync());
        Assert.Equal(14, await context.Database.SqlQuery<int>($"""
            SELECT count(*)::integer AS "Value" FROM "AchievementDefinitions"
            """).SingleAsync());
        Assert.True(await context.Database.SqlQuery<bool>($"""
            SELECT "NotBeforeUtc" > NOW() + INTERVAL '4 minutes' AS "Value" FROM "TelegramAssertionPolicies"
            """).SingleAsync());
        // The script validates columns, constraints, indexes, sequences and extensions.
        await AdoptAsync(context);
        await context.Database.MigrateAsync();
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyDatabase_AdoptionPreservesEveryApplicationRowAndIsRepeatable(bool hasRemovedSeedMarker) {
        await using FoodDiaryDbContext context = await CreateLegacyDatabaseAsync();
        if (hasRemovedSeedMarker) {
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "__EFMigrationsHistory" VALUES ('20260208133129_SeedEmailTemplates', '10.0.11');
                """);
        }
        var user = User.Create("baseline-preservation@example.com", "test-hash");
        user.SetLanguage("ru");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("""
            UPDATE "EmailTemplates" SET "HtmlBody" = '<p>Custom template / Свой шаблон</p>';
            INSERT INTO "ConsumedTelegramAssertions" ("Fingerprint", "ExpiresAtUtc")
                VALUES ('v2:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef', NOW() + INTERVAL '1 hour');
            """);
        string before = await ReadApplicationDataAsync(context);

        await AdoptAsync(context);
        await AdoptAsync(context);

        Assert.Equal(before, await ReadApplicationDataAsync(context));
        Assert.Equal([Baseline], await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        await context.Database.MigrateAsync();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [RequiresDockerTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LegacyDatabase_IncompleteOrUnknownHistoryIsRejectedAtomically(bool unknownMigration, bool additionalMigration) {
        await using FoodDiaryDbContext context = await CreateLegacyDatabaseAsync();
        if (!additionalMigration) {
            await context.Database.ExecuteSqlRawAsync("""
                DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260209005246_AddShoppingLists';
                """);
        }
        if (unknownMigration) {
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "__EFMigrationsHistory" VALUES ('20260209005246_UnknownMigration', '10.0.11');
                """);
        }
        string[] history = [.. await context.Database.GetAppliedMigrationsAsync()];
        string before = await ReadApplicationDataAsync(context);

        PostgresException failure = await Assert.ThrowsAsync<PostgresException>(() => AdoptAsync(context));

        Assert.Contains("Incomplete or unknown legacy migration history", failure.MessageText, StringComparison.Ordinal);
        Assert.Equal(history, await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(before, await ReadApplicationDataAsync(context));
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyCoexistingSchema_AdoptionPreservesEveryApplicationRowAndPrismaHistory(bool hasRemovedSeedMarker) {
        await using FoodDiaryDbContext context = await CreateLegacyDatabaseAsync();
        await ExecuteFixtureAsync(context, "LegacyCoexistingSchema.sql");
        if (hasRemovedSeedMarker) {
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "__EFMigrationsHistory" VALUES ('20260208133129_SeedEmailTemplates', '10.0.11');
                """);
        }
        context.Users.Add(User.Create("coexisting-baseline@example.com", "test-hash"));
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO _prisma_migrations (id, checksum, migration_name)
                VALUES ('00000000-0000-0000-0000-000000000001', 'legacy-test-checksum', 'preserved_legacy_history');
            INSERT INTO mailrelay_suppressions (email, reason, source, created_at_utc, updated_at_utc)
                VALUES ('legacy-baseline@example.com', 'test fixture', 'test', NOW(), NOW());
            """);
        string before = await ReadApplicationDataAsync(context);

        await AdoptAsync(context);
        await AdoptAsync(context);
        await context.Database.MigrateAsync();

        Assert.Equal(before, await ReadApplicationDataAsync(context));
        Assert.Equal(context.Database.GetMigrations(), await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [RequiresDockerTheory]
    [InlineData("DROP INDEX \"IX_BillingPayments_EffectiveOccurredAtUtc\";")]
    [InlineData("ALTER TABLE \"Users\" ALTER COLUMN \"HasPassword\" DROP NOT NULL;")]
    [InlineData("ALTER TABLE \"Food\" ALTER COLUMN name DROP NOT NULL;")]
    [InlineData("CREATE TABLE \"UnexpectedLegacyObject\" (id integer);")]
    [InlineData("DROP TABLE mailrelay_suppressions;")]
    public async Task LegacyCoexistingSchema_UnexpectedDriftIsRejectedAtomically(string driftSql) {
        await using FoodDiaryDbContext context = await CreateLegacyDatabaseAsync();
        await ExecuteFixtureAsync(context, "LegacyCoexistingSchema.sql");
        await context.Database.ExecuteSqlRawAsync(driftSql);
        string[] history = [.. await context.Database.GetAppliedMigrationsAsync()];
        string before = await ReadApplicationDataAsync(context);

        PostgresException failure = await Assert.ThrowsAsync<PostgresException>(() => AdoptAsync(context));

        Assert.Contains("Schema differs", failure.MessageText, StringComparison.Ordinal);
        Assert.Equal(history, await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(before, await ReadApplicationDataAsync(context));
    }

    [RequiresDockerFact]
    public async Task NativeBaseline_CoexistingSchemaRetainsItsOriginalExpressionForms() {
        await using FoodDiaryDbContext context = await CreateBaselineDatabaseAsync();
        await ExecuteFixtureAsync(context, "LegacyCoexistingSchema.sql");
        string before = await ReadApplicationDataAsync(context);

        await AdoptAsync(context);

        Assert.Equal(before, await ReadApplicationDataAsync(context));
        Assert.Equal([Baseline], await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(context.Database.GetMigrations().Skip(1), await context.Database.GetPendingMigrationsAsync(), StringComparer.Ordinal);
    }

    [RequiresDockerTheory]
    [InlineData("DROP INDEX \"IX_BillingPayments_EffectiveOccurredAtUtc\";")]
    [InlineData("ALTER TABLE \"FastingCheckIns\" DROP CONSTRAINT \"FK_FastingCheckIns_Users_UserId\";")]
    [InlineData("ALTER TABLE \"Users\" ALTER COLUMN \"HasPassword\" DROP NOT NULL;")]
    public async Task LegacyDatabase_SchemaDriftIsRejectedWithoutChangingHistory(string driftSql) {
        await using FoodDiaryDbContext context = await CreateLegacyDatabaseAsync();
        await context.Database.ExecuteSqlRawAsync(driftSql);
        string[] history = [.. await context.Database.GetAppliedMigrationsAsync()];

        PostgresException failure = await Assert.ThrowsAsync<PostgresException>(() => AdoptAsync(context));

        Assert.Contains("Schema differs", failure.MessageText, StringComparison.Ordinal);
        Assert.Equal(history, await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Postgres18_AdoptionPreservesRowsAndStillRejectsNullabilityDrift(bool hasCoexistingSchema) {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("fooddiary_baseline")
            .WithUsername("postgres")
            .WithPassword(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)))
            .WithPortBinding(5432, assignRandomHostPort: true)
            .Build();
        await postgres.StartAsync();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(postgres.GetConnectionString());
        await ExecuteFixtureAsync(context, "PreBaseline.sql");
        if (hasCoexistingSchema) {
            await ExecuteFixtureAsync(context, "LegacyCoexistingSchema.sql");
        }
        string before = await ReadApplicationDataAsync(context);

        await AdoptAsync(context);

        Assert.Equal(before, await ReadApplicationDataAsync(context));
        Assert.Equal([Baseline], await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ALTER COLUMN \"HasPassword\" DROP NOT NULL;");

        PostgresException failure = await Assert.ThrowsAsync<PostgresException>(() => AdoptAsync(context));

        Assert.Contains("Schema differs", failure.MessageText, StringComparison.Ordinal);
        Assert.Equal([Baseline], await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
    }

    [RequiresDockerFact]
    public async Task LegacyDatabase_OrdinaryUpdateRequiresExplicitAdoption() {
        await using FoodDiaryDbContext context = await CreateLegacyDatabaseAsync();
        string before = await ReadApplicationDataAsync(context);
        string[] history = [.. await context.Database.GetAppliedMigrationsAsync()];

        PostgresException failure = await Assert.ThrowsAsync<PostgresException>(() => context.Database.MigrateAsync());

        Assert.Contains("Adopt-InitialBaseline.sql", failure.MessageText, StringComparison.Ordinal);
        Assert.Equal(history, await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
        Assert.Equal(before, await ReadApplicationDataAsync(context));
    }

    [RequiresDockerFact]
    public async Task BaselineRollback_IsRejectedWithoutDroppingApplicationTables() {
        await using FoodDiaryDbContext context = await CreateBaselineDatabaseAsync();
        string before = await ReadApplicationDataAsync(context);

        await Assert.ThrowsAsync<NotSupportedException>(() => context.GetService<IMigrator>().MigrateAsync("0"));

        Assert.Equal(before, await ReadApplicationDataAsync(context));
        Assert.Equal([Baseline], await context.Database.GetAppliedMigrationsAsync(), StringComparer.Ordinal);
    }

    [RequiresDockerFact]
    public async Task CleanDatabase_ReferenceContentMatchesTheFrozenLegacyDatabase() {
        await using FoodDiaryDbContext legacy = await CreateLegacyDatabaseAsync();
        await using FoodDiaryDbContext fresh = await CreateBaselineDatabaseAsync();

        Assert.Equal(await ReadReferenceContentAsync(legacy), await ReadReferenceContentAsync(fresh), StringComparer.Ordinal);
    }

    [RequiresDockerFact]
    public async Task IdempotentScript_PreservesReferenceContentAndCanRunTwice() {
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(await databaseFixture.CreateIsolatedDatabaseAsync());
        string script = context.GetService<IMigrator>().GenerateScript(toMigration: Baseline, options: MigrationsSqlGenerationOptions.Idempotent);

        await ExecuteSqlAsync(context, script);
        string before = await ReadApplicationDataAsync(context);
        await ExecuteSqlAsync(context, script);

        Assert.Equal(before, await ReadApplicationDataAsync(context));
        await using FoodDiaryDbContext legacy = await CreateLegacyDatabaseAsync();
        Assert.Equal(await ReadReferenceContentAsync(legacy), await ReadReferenceContentAsync(context), StringComparer.Ordinal);
        await AdoptAsync(context);
    }

    private static async Task<string[]> ReadReferenceContentAsync(FoodDiaryDbContext context) {
        // Installation-time audit timestamps vary; reference identities and all content must match.
        const string query = """
            SELECT jsonb_build_object('table', 'advice', 'row', to_jsonb(t))::text AS "Value" FROM "DailyAdvices" t
            UNION ALL SELECT jsonb_build_object('table', 'template', 'row', to_jsonb(t) - 'CreatedOnUtc' - 'ModifiedOnUtc')::text FROM "EmailTemplates" t
            UNION ALL SELECT jsonb_build_object('table', 'achievement', 'row', to_jsonb(t) - 'CreatedOnUtc')::text FROM "AchievementDefinitions" t
            UNION ALL SELECT jsonb_build_object('table', 'role', 'row', to_jsonb(t) - 'CreatedOnUtc')::text FROM "Roles" t
            """;
        return await context.Database.SqlQueryRaw<string>(query).OrderBy(value => value).ToArrayAsync();
    }

    [Fact]
    public void MigrationTypes_AreExcludedFromCodeCoverage() {
        Type[] types = [.. typeof(FoodDiaryDbContext).Assembly.GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type) || typeof(ModelSnapshot).IsAssignableFrom(type))];
        Assert.NotEmpty(types);
        Assert.All(types, type => Assert.True(Attribute.IsDefined(type, typeof(ExcludeFromCodeCoverageAttribute))));
    }

    private async Task<FoodDiaryDbContext> CreateLegacyDatabaseAsync() {
        FoodDiaryDbContext context = databaseFixture.CreateDbContext(await databaseFixture.CreateIsolatedDatabaseAsync());
        try {
            await context.Database.OpenConnectionAsync();
            await ExecuteFixtureAsync(context, "PreBaseline.sql");
            return context;
        } catch {
            await context.DisposeAsync();
            throw;
        }
    }

    private async Task<FoodDiaryDbContext> CreateBaselineDatabaseAsync() {
        FoodDiaryDbContext context = databaseFixture.CreateDbContext(await databaseFixture.CreateIsolatedDatabaseAsync());
        try {
            await context.GetService<IMigrator>().MigrateAsync(Baseline);
            return context;
        } catch {
            await context.DisposeAsync();
            throw;
        }
    }

    private static Task AdoptAsync(FoodDiaryDbContext context) =>
        ExecuteFixtureAsync(context, "Adopt-InitialBaseline.sql");

    private static async Task ExecuteFixtureAsync(FoodDiaryDbContext context, string name) {
        await ExecuteSqlAsync(context, await ReadFixtureAsync(name));
    }

    private static async Task ExecuteSqlAsync(FoodDiaryDbContext context, string sql) {
        if (context.Database.GetDbConnection().State != System.Data.ConnectionState.Open) {
            await context.Database.OpenConnectionAsync();
        }
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        // Execute the captured SQL verbatim; HTML/JSON braces are not format placeholders.
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static Task<string> ReadFixtureAsync(string name) =>
        File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    private static async Task<string> ReadApplicationDataAsync(FoodDiaryDbContext context) {
        string[] tables = await context.Database.SqlQuery<string>($"""
            SELECT tablename::text AS "Value" FROM pg_tables
            WHERE schemaname = 'public' AND tablename <> '__EFMigrationsHistory' ORDER BY tablename
            """).ToArrayAsync();
        var rows = new List<string>();
        foreach (string table in tables) {
            string quotedTable = table.Replace("\"", "\"\"", StringComparison.Ordinal);
            string query = $"SELECT COALESCE(jsonb_agg(to_jsonb(t) ORDER BY to_jsonb(t)::text COLLATE \"C\"), '[]'::jsonb)::text AS \"Value\" FROM public.\"{quotedTable}\" t";
            rows.Add(table + ":" + await context.Database.SqlQueryRaw<string>(query).SingleAsync());
        }
        return string.Join('\n', rows);
    }
}
