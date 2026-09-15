using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
using FoodDiary.Modules.Lessons.Domain.Contracts.Enums;
using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.ReadModel.Composition.Gamification;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Lessons.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.PersistenceModel.Achievements;
using FoodDiary.Modules.Gamification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

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
        var firstStore = new AchievementDefinitionStore(firstContext, firstContext.AchievementDefinitions, firstContext.UserAchievements);
        var secondStore = new AchievementDefinitionStore(secondContext, secondContext.AchievementDefinitions, secondContext.UserAchievements);
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
            var outbox = new AchievementEvaluationOutbox(enqueueContext, enqueueContext.AchievementEvaluationOutbox, timeProvider);
            await outbox.EnqueueAsync(user.Id);
            await enqueueContext.SaveChangesAsync();
        }

        ISender handler = Substitute.For<ISender>();
        await using FoodDiaryDbContext processContext = databaseFixture.CreateDbContext(connectionString);
        var processor = new AchievementEvaluationOutboxProcessor(
            processContext, processContext.AchievementEvaluationOutbox,
            handler,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            timeProvider,
            NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);
        await handler.Received(1).Send(new ReconcileAchievementsCommand(user.Id, timeProvider.GetUtcNow().UtcDateTime), Arg.Any<CancellationToken>());
        Assert.NotNull((await processContext.AchievementEvaluationOutbox.SingleAsync()).ProcessedOnUtc);
    }

    [RequiresDockerFact]
    public async Task AchievementEvaluationOutbox_RepeatedRequests_CoalescePerUser() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        var user = User.Create($"achievement-coalesce-{Guid.NewGuid():N}@example.com", "hash");
        var timeProvider = new StubTimeProvider();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);
        await context.Database.MigrateAsync();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var outbox = new AchievementEvaluationOutbox(context, context.AchievementEvaluationOutbox, timeProvider);

        await outbox.EnqueueAsync(user.Id);
        await outbox.EnqueueAsync(user.Id);

        AchievementEvaluationOutboxMessage message = await context.AchievementEvaluationOutbox.SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(user.Id, message.UserId),
            () => Assert.Equal(2, message.Revision),
            () => Assert.Null(message.ProcessedOnUtc));
    }

    [RequiresDockerFact]
    public async Task AchievementEvaluationOutbox_RequestDuringProcessing_RemainsPending() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        var user = User.Create($"achievement-race-{Guid.NewGuid():N}@example.com", "hash");
        var timeProvider = new StubTimeProvider();
        await using (FoodDiaryDbContext setupContext = databaseFixture.CreateDbContext(connectionString)) {
            await setupContext.Database.MigrateAsync();
            setupContext.Users.Add(user);
            await setupContext.SaveChangesAsync();
            await new AchievementEvaluationOutbox(setupContext, setupContext.AchievementEvaluationOutbox, timeProvider).EnqueueAsync(user.Id);
        }

        ISender handler = Substitute.For<ISender>();
        handler.Send(Arg.Is<ReconcileAchievementsCommand>(q => q.UserId == user.Id), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                await using FoodDiaryDbContext concurrentContext = databaseFixture.CreateDbContext(connectionString);
                await new AchievementEvaluationOutbox(concurrentContext, concurrentContext.AchievementEvaluationOutbox, timeProvider).EnqueueAsync(user.Id);
            });
        await using FoodDiaryDbContext processContext = databaseFixture.CreateDbContext(connectionString);
        var processor = new AchievementEvaluationOutboxProcessor(
            processContext, processContext.AchievementEvaluationOutbox,
            handler,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            timeProvider,
            NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 1);

        AchievementEvaluationOutboxMessage message = await processContext.AchievementEvaluationOutbox
            .AsNoTracking()
            .SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(2, message.Revision),
            () => Assert.Null(message.ProcessedOnUtc),
            () => Assert.Null(message.LockedUntilUtc));
    }

    [RequiresDockerFact]
    public async Task ManagedDefinitions_AreSeededAndActiveStoreReflectsUpdates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var store = new AchievementDefinitionStore(context, context.AchievementDefinitions, context.UserAchievements);

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
            () => Assert.Equal(14, seeded.Count),
            () => Assert.Contains(seeded, item =>
                string.Equals(item.Key, "academy_articles_25", StringComparison.Ordinal) &&
                item.Metric == AchievementMetric.TotalAcademyArticlesRead),
            () => Assert.DoesNotContain(active, item => string.Equals(item.Key, "streak_3", StringComparison.Ordinal)),
            () => Assert.Equal(2, definition.Version));
    }

    [RequiresDockerFact]
    public async Task AchievementMetricReader_CountsOnlyCompletedArticlesForRequestedUser() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"academy-reader-{Guid.NewGuid():N}@example.com", "hash");
        var otherUser = User.Create($"academy-other-{Guid.NewGuid():N}@example.com", "hash");
        var firstLesson = NutritionLesson.Create(
            "First", "Content", summary: null, "en", LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        var secondLesson = NutritionLesson.Create(
            "Second", "Content", summary: null, "en", LessonCategory.NutritionBasics, LessonDifficulty.Beginner, 3);
        context.Users.AddRange(user, otherUser);
        context.NutritionLessons.AddRange(firstLesson, secondLesson);
        context.UserLessonProgress.AddRange(
            UserLessonProgress.Create(user.Id, firstLesson.Id, DateTime.UtcNow),
            UserLessonProgress.Create(user.Id, secondLesson.Id, DateTime.UtcNow),
            UserLessonProgress.Create(otherUser.Id, firstLesson.Id, DateTime.UtcNow));
        await context.SaveChangesAsync();
        var reader = new AchievementMetricReader(context);

        int completedArticles = await reader.GetCompletedAcademyArticleCountAsync(user.Id);

        Assert.Equal(2, completedArticles);
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
        var firstStore = new UserAchievementStore(firstContext, firstContext.UserAchievements);
        var secondStore = new UserAchievementStore(secondContext, secondContext.UserAchievements);

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
        IReadOnlyDictionary<string, int> counts = await new AchievementDefinitionStore(assertionContext, assertionContext.AchievementDefinitions, assertionContext.UserAchievements).GetAwardCountsAsync();
        Assert.Equal(1, counts["streak-3"]);
        Assert.Single(counts);
        assertionContext.AchievementDefinitions.AddRange(CreateDefinition("streak-3"), CreateDefinition("unawarded"));
        await assertionContext.SaveChangesAsync();
        assertionContext.ChangeTracker.Clear();
        var reader = new AchievementDefinitionStore(assertionContext, assertionContext.AchievementDefinitions, assertionContext.UserAchievements);
        IReadOnlyList<AchievementDefinitionAdminModel> models = await reader.GetForAdministrationAsync();
        AchievementDefinitionAdminModel awarded = Assert.Single(models, item => string.Equals(item.Key, "streak-3", StringComparison.Ordinal));
        AchievementDefinitionAdminModel unawarded = Assert.Single(models, item => string.Equals(item.Key, "unawarded", StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.Equal(1, awarded.AwardedUsers),
            () => Assert.Equal(0, unawarded.AwardedUsers),
            () => Assert.Equal("TotalMeals", awarded.Metric),
            () => Assert.Equal("Title", awarded.TitleEn),
            () => Assert.Empty(assertionContext.ChangeTracker.Entries()));
    }

    private static AchievementDefinition CreateDefinition(string key) => AchievementDefinition.Create(
        key, "habits", FoodDiary.Modules.Gamification.Domain.Contracts.Enums.AchievementMetric.TotalMeals, 10,
        "Название", "Title", "Описание", "Description", "trophy", 1);

    [ExcludeFromCodeCoverage]
    private sealed class StubTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
    }
}
