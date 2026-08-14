using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Authentication;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Recommendations;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DietologistPersistenceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [RequiresDockerFact]
    public async Task RecommendationRepositories_AddAndQueryEveryShape() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var dietologist = User.Create($"diet-{Guid.NewGuid():N}@example.com", "hash");
        var client = User.Create($"client-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(dietologist, client);
        await context.SaveChangesAsync();

        var recommendation = Recommendation.Create(dietologist.Id, client.Id, "Recommendation");
        context.Recommendations.Add(recommendation);
        await context.SaveChangesAsync();

        var commentRepository = new RecommendationCommentRepository(context);
        RecommendationComment comment = await commentRepository.AddAsync(
            RecommendationComment.Create(recommendation.Id, client.Id, "Comment"));
        var taskRepository = new ClientTaskRepository(context);
        ClientTask task = await taskRepository.AddAsync(
            ClientTask.Create(dietologist.Id, client.Id, "Task", "Details", UtcNow.AddHours(1)));
        var templateRepository = new RecommendationTemplateRepository(context);
        RecommendationTemplate template = await templateRepository.AddAsync(
            RecommendationTemplate.Create(dietologist.Id, "Template", "Text"));
        var dispatchRepository = new RecommendationBulkDispatchRepository(context);
        RecommendationBulkDispatch dispatch = await dispatchRepository.AddAsync(
            RecommendationBulkDispatch.Create(dietologist.Id, client.Id, recommendation.Id, "key"));
        await context.SaveChangesAsync();

        Assert.NotNull(await taskRepository.GetByIdAsync(task.Id));
        Assert.NotNull(await taskRepository.GetByIdAsync(task.Id, asTracking: true));
        Assert.Single(await taskRepository.GetByClientAsync(client.Id));
        Assert.Single(await taskRepository.GetByDietologistAndClientAsync(dietologist.Id, client.Id));
        Assert.Single(await taskRepository.GetDueForReminderAsync(UtcNow, UtcNow.AddHours(2), 10));
        Assert.Single(await commentRepository.GetByRecommendationAsync(recommendation.Id));
        Assert.NotNull(await templateRepository.GetByIdAsync(template.Id));
        Assert.NotNull(await templateRepository.GetByIdAsync(template.Id, asTracking: true));
        Assert.Single(await templateRepository.SearchAsync(dietologist.Id, search: null, includeArchived: false));
        Assert.Single(await templateRepository.SearchAsync(dietologist.Id, "plate", includeArchived: true));
        Assert.Single(await dispatchRepository.GetExistingAsync(dietologist.Id, "key", [client.Id]));
    }

    [RequiresDockerFact]
    public async Task AttentionSignalMetricsReadService_BatchesMultipleClientsAgainstPostgres() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var first = User.Create($"attention-first-{Guid.NewGuid():N}@example.com", "hash");
        var second = User.Create($"attention-second-{Guid.NewGuid():N}@example.com", "hash");
        var firstMeal = Meal.Create(first.Id, UtcNow.AddDays(-2));
        var secondMeal = Meal.Create(second.Id, UtcNow.AddDays(-1));
        var firstWeight = WeightEntry.Create(first.Id, UtcNow.AddDays(-3), 90);
        var secondWeight = WeightEntry.Create(second.Id, UtcNow.AddDays(-2), 75);
        context.AddRange(first, second, firstMeal, secondMeal, firstWeight, secondWeight);
        await context.SaveChangesAsync();
        var service = new AttentionSignalMetricsReadService(context);

        IReadOnlyList<AttentionSignalMetricsReadModel> result = await service.GetAsync(
            [first.Id, second.Id],
            UtcNow.AddDays(-7),
            UtcNow);

        Assert.Multiple(
            () => Assert.Equal(2, result.Count),
            () => Assert.Contains(result, item =>
                item.ClientUserId == first.Id.Value &&
                item.LastMealAtUtc == firstMeal.Date &&
                item.WeightPoints.Single().Weight == firstWeight.Weight),
            () => Assert.Contains(result, item =>
                item.ClientUserId == second.Id.Value &&
                item.LastMealAtUtc == secondMeal.Date &&
                item.WeightPoints.Single().Weight == secondWeight.Weight));
    }

    [RequiresDockerFact]
    public async Task AuditEntryService_AddsFiltersAndProjectsEntries() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var service = new AuditEntryService(context, FixedTime);
        var actor = FoodDiary.Domain.ValueObjects.Ids.UserId.New();
        var subject = Guid.NewGuid();

        await service.AddAsync(actor, subject, "action", "Target", "id", """{"value":1}""");
        await service.AddAsync(
            actor,
            subjectClientUserId: null,
            action: "other",
            targetType: "Target",
            targetId: null,
            metadata: null);
        await context.SaveChangesAsync();

        Assert.Equal(2, (await service.GetRecentAsync(subjectClientUserId: null, limit: 10)).Count);
        AuditEntryReadModel filtered = Assert.Single(await service.GetRecentAsync(subject, 10));
        Assert.Multiple(
            () => Assert.Equal(actor.Value, filtered.ActorUserId),
            () => Assert.Equal(subject, filtered.SubjectClientUserId),
            () => Assert.Equal("action", filtered.Action),
            () => Assert.Equal("Target", filtered.TargetType),
            () => Assert.Equal("id", filtered.TargetId),
            () => Assert.Equal("""{"value":1}""", filtered.Metadata),
            () => Assert.Equal(UtcNow, filtered.CreatedAtUtc));
    }

    [RequiresDockerFact]
    public async Task TelegramAssertionReplayGuard_ConsumesAssertionOnlyOnceAndDeletesExpiredRows() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var guard = new TelegramAssertionReplayGuard(context, FixedTime);

        bool first = await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5));
        bool duplicate = await guard.TryConsumeAsync("signed-assertion", UtcNow.AddMinutes(5));
        bool expired = await guard.TryConsumeAsync("expired-assertion", UtcNow);
        bool afterCleanup = await guard.TryConsumeAsync("another-assertion", UtcNow.AddMinutes(5));

        Assert.Multiple(
            () => Assert.True(first),
            () => Assert.False(duplicate),
            () => Assert.True(expired),
            () => Assert.True(afterCleanup));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }
}
