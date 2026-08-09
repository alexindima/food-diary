using FoodDiary.Application.Abstractions.Achievements.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Achievements;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Application.Abstractions.Achievements.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserAchievementStoreIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task AchievementDefinitionStore_ConcurrentDuplicateKey_ReturnsOneConflictWithoutException() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using (FoodDiaryDbContext migrationContext = databaseFixture.CreateDbContext(connectionString)) {
            await migrationContext.Database.MigrateAsync();
        }

        await using FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(connectionString);
        var firstStore = new AchievementDefinitionStore(firstContext);
        var secondStore = new AchievementDefinitionStore(secondContext);
        AchievementDefinition first = CreateDefinition("concurrent_key");
        AchievementDefinition second = CreateDefinition("concurrent_key");

        bool[] results = await Task.WhenAll(firstStore.TryAddAsync(first), secondStore.TryAddAsync(second));

        Assert.Multiple(
            () => Assert.Single(results, value => value),
            () => Assert.Single(results, value => !value));
    }

    [RequiresDockerFact]
    public async Task AchievementDefinitionStore_ConcurrentUpdate_RejectsLostUpdate() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using (FoodDiaryDbContext migrationContext = databaseFixture.CreateDbContext(connectionString)) {
            await migrationContext.Database.MigrateAsync();
        }

        await using FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(connectionString);
        AchievementDefinition first = await firstContext.AchievementDefinitions.SingleAsync(
            item => string.Equals(item.Key, "meals_10"));
        AchievementDefinition second = await secondContext.AchievementDefinitions.SingleAsync(
            item => string.Equals(item.Key, "meals_10"));
        first.Update(first.Category, first.Metric, 11, first.TitleRu, first.TitleEn,
            first.DescriptionRu, first.DescriptionEn, first.Icon, first.SortOrder, first.IsActive);
        second.Update(second.Category, second.Metric, 12, second.TitleRu, second.TitleEn,
            second.DescriptionRu, second.DescriptionEn, second.Icon, second.SortOrder, second.IsActive);

        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => secondContext.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task AchievementEvaluationOutbox_ProcessesDurableMessage() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        var user = User.Create($"achievement-outbox-{Guid.NewGuid():N}@example.com", "hash");
        var timeProvider = new StubTimeProvider();
        await using (FoodDiaryDbContext enqueueContext = databaseFixture.CreateDbContext(connectionString)) {
            await enqueueContext.Database.MigrateAsync();
            enqueueContext.Users.Add(user);
            var outbox = new AchievementEvaluationOutbox(enqueueContext, timeProvider);
            await outbox.EnqueueAsync(user.Id);
            await enqueueContext.SaveChangesAsync();
        }

        IAchievementReconciliationHandler handler = Substitute.For<IAchievementReconciliationHandler>();
        await using FoodDiaryDbContext processContext = databaseFixture.CreateDbContext(connectionString);
        var processor = new AchievementEvaluationOutboxProcessor(
            processContext, handler, timeProvider, NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);
        await handler.Received(1).ReconcileAsync(user.Id, timeProvider.GetUtcNow().UtcDateTime, Arg.Any<CancellationToken>());
        Assert.NotNull((await processContext.AchievementEvaluationOutbox.SingleAsync()).ProcessedOnUtc);
    }

    [RequiresDockerFact]
    public async Task ManagedDefinitions_AreSeededAndActiveStoreReflectsUpdates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var store = new AchievementDefinitionStore(context);

        IReadOnlyList<AchievementDefinition> seeded = await store.GetActiveAsync();
        AchievementDefinition definition = seeded.Single(item => string.Equals(item.Key, "streak_3", StringComparison.Ordinal));
        definition = await store.GetByIdTrackingAsync(definition.Id) ?? throw new InvalidOperationException("Seeded definition not found.");
        definition.Update(
            definition.Category, definition.Metric, definition.Threshold,
            definition.TitleRu, definition.TitleEn, definition.DescriptionRu, definition.DescriptionEn,
            definition.Icon, definition.SortOrder, isActive: false);
        await store.UpdateAsync(definition);
        await context.SaveChangesAsync();

        IReadOnlyList<AchievementDefinition> active = await store.GetActiveAsync();
        Assert.Multiple(
            () => Assert.Equal(10, seeded.Count),
            () => Assert.DoesNotContain(active, item => string.Equals(item.Key, "streak_3", StringComparison.Ordinal)),
            () => Assert.Equal(2, definition.Version));
    }

    [RequiresDockerFact]
    public async Task GrantMissingAsync_ConcurrentDuplicateGrant_IsPersistedOnce() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        var earnedAtUtc = new DateTime(2030, 7, 9, 12, 0, 0, DateTimeKind.Utc);
        var user = User.Create($"achievements-{Guid.NewGuid():N}@example.com", "hash");

        await using (FoodDiaryDbContext migrationContext = databaseFixture.CreateDbContext(connectionString)) {
            await migrationContext.Database.MigrateAsync();
            migrationContext.Users.Add(user);
            await migrationContext.SaveChangesAsync();
        }

        AchievementGrantModel[] grants = [new(
            AchievementKey: "streak-3",
            earnedAtUtc,
            EarnedValue: 3,
            DefinitionVersion: 1)];
        await using FoodDiaryDbContext firstContext = databaseFixture.CreateDbContext(connectionString);
        await using FoodDiaryDbContext secondContext = databaseFixture.CreateDbContext(connectionString);
        var firstStore = new UserAchievementStore(firstContext);
        var secondStore = new UserAchievementStore(secondContext);

        await Task.WhenAll(
            firstStore.GrantMissingAsync(user.Id, grants),
            secondStore.GrantMissingAsync(user.Id, grants));

        await using FoodDiaryDbContext assertionContext = databaseFixture.CreateDbContext(connectionString);
        UserAchievement persisted = Assert.Single(
            await assertionContext.UserAchievements.AsNoTracking().ToListAsync());
        Assert.Multiple(
            () => Assert.Equal(user.Id, persisted.UserId),
            () => Assert.Equal("streak-3", persisted.AchievementKey),
            () => Assert.Equal(earnedAtUtc, persisted.EarnedAtUtc),
            () => Assert.Equal(3, persisted.EarnedValue),
            () => Assert.Equal(1, persisted.DefinitionVersion));
    }

    private static AchievementDefinition CreateDefinition(string key) => AchievementDefinition.Create(
        key, "habits", FoodDiary.Domain.Enums.AchievementMetric.TotalMeals, 10,
        "Название", "Title", "Описание", "Description", "trophy", 1);

    [ExcludeFromCodeCoverage]
    private sealed class StubTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    }
}
