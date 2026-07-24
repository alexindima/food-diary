using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Authentication;
using FoodDiary.Infrastructure.Persistence.Recommendations;

namespace FoodDiary.Infrastructure.Tests.Integration;

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
