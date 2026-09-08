using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Events;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Infrastructure.Persistence.Dietologist;
using FoodDiary.Infrastructure.Persistence.Recommendations;
using FoodDiary.Modules.Dietologist.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DietologistPersistenceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime UtcNow = new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [RequiresDockerFact]
    public async Task AuditJournal_FiltersBeforePaginationAndUsesExclusiveEnd() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var actor = Guid.NewGuid();
        var client = Guid.NewGuid();
        DateTime start = UtcNow.Date;
        var expectedId = Guid.NewGuid();
        context.AuditEntries.AddRange(
            new AuditEntry { Id = expectedId, ActorUserId = actor, SubjectClientUserId = client, Action = "test.action", TargetType = "Test", CreatedAtUtc = start },
            new AuditEntry { Id = Guid.NewGuid(), ActorUserId = actor, SubjectClientUserId = client, Action = "test.action", TargetType = "Test", CreatedAtUtc = start.AddHours(1) },
            new AuditEntry { Id = Guid.NewGuid(), ActorUserId = actor, SubjectClientUserId = client, Action = "test.action", TargetType = "Test", CreatedAtUtc = start.AddDays(1) },
            new AuditEntry { Id = Guid.NewGuid(), ActorUserId = Guid.NewGuid(), SubjectClientUserId = client, Action = "test.action", TargetType = "Test", CreatedAtUtc = start });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var service = new AuditEntryService(context, FixedTime);
        var filter = new AuditEntryFilter(Page: 2, Limit: 1, FromUtc: new DateTimeOffset(start), ToUtc: new DateTimeOffset(start.AddDays(1)),
            ActorUserId: actor, SubjectClientUserId: client, Action: "test.action", TargetType: "Test", TargetId: null);

        AuditEntryPage result = await service.GetPageAsync(filter, CancellationToken.None);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(expectedId, Assert.Single(result.Items).Id);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [RequiresDockerFact]
    public async Task ModuleAudit_ComposesAfterDomainEvents_AndSharesCommitAndRollback() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var dietologist = User.Create($"audit-diet-{Guid.NewGuid():N}@example.com", "hash");
        var client = User.Create($"audit-client-{Guid.NewGuid():N}@example.com", "hash");
        database.Users.AddRange(dietologist, client);
        await database.SaveChangesAsync();

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = database.Database.GetConnectionString(),
                ["Database:EnableRetries"] = "false",
            }).Build();
        var services = new ServiceCollection();
        services.AddSingleton(FixedTime);
        services.AddInfrastructure(configuration);
        services.AddDietologistModule().AddDietologistModule();
        services.AddScoped(_ => Substitute.For<IDomainEventPublisher>());
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var invitation = DietologistInvitation.Create(
            client.Id, dietologist.Email, "token-hash", DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled);
        context.DietologistInvitations.Add(invitation);
        SaveSynchronously(context);

        AuditEntry created = Assert.Single(await database.AuditEntries.AsNoTracking().ToListAsync());
        Assert.Multiple(
            () => Assert.Equal(client.Id.Value, created.ActorUserId),
            () => Assert.Equal(client.Id.Value, created.SubjectClientUserId),
            () => Assert.Equal("dietologist.invitation.created", created.Action),
            () => Assert.Equal(invitation.Id.Value.ToString(), created.TargetId),
            () => Assert.Equal(UtcNow, created.CreatedAtUtc),
            () => Assert.Null(created.Metadata));

        IDomainEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();
        publisher.PublishAsync(Arg.Is<IDomainEvent>(domainEvent => domainEvent is DietologistInvitationAcceptedDomainEvent), Arg.Any<CancellationToken>()).Returns(_ => {
            context.Recommendations.Add(Recommendation.Create(dietologist.Id, client.Id, "Created during dispatch"));
            return Task.CompletedTask;
        });

        await using (IDbContextTransaction transaction = await context.Database.BeginTransactionAsync()) {
            invitation.Accept(dietologist.Id);
            await context.SaveChangesAsync();

            await publisher.Received(1).PublishAsync(
                Arg.Is<IDomainEvent>(domainEvent => domainEvent is DietologistInvitationAcceptedDomainEvent), Arg.Any<CancellationToken>());
            await publisher.Received(1).PublishAsync(
                Arg.Is<IDomainEvent>(domainEvent => domainEvent is RecommendationCreatedDomainEvent), Arg.Any<CancellationToken>());
            AuditEntry[] transactionalAudit = await context.AuditEntries.AsNoTracking().ToArrayAsync();
            Assert.Equal(3, transactionalAudit.Length);
            Assert.Contains(transactionalAudit, entry => string.Equals(entry.Action, "dietologist.recommendation.created", StringComparison.Ordinal));
            AuditEntry accepted = Assert.Single(transactionalAudit, entry => string.Equals(entry.Action, "dietologist.invitation.accepted", StringComparison.Ordinal));
            Assert.Multiple(
                () => Assert.Equal(dietologist.Id.Value, accepted.ActorUserId),
                () => Assert.Equal(client.Id.Value, accepted.SubjectClientUserId),
                () => Assert.Equal("""{"status":"Accepted"}""", accepted.Metadata),
                () => Assert.Equal(UtcNow, accepted.CreatedAtUtc));
            Assert.Equal(1, await database.AuditEntries.CountAsync());
            Assert.Empty(await database.Recommendations.ToListAsync());
            await transaction.RollbackAsync();
        }

        Assert.Equal(1, await database.AuditEntries.CountAsync());
        Assert.Empty(await database.Recommendations.ToListAsync());
        Assert.Equal(DietologistInvitationStatus.Pending,
            (await database.DietologistInvitations.AsNoTracking().SingleAsync()).Status);

        await using AsyncServiceScope committedScope = provider.CreateAsyncScope();
        FoodDiaryDbContext committedContext = committedScope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        DietologistInvitation committed = await committedContext.DietologistInvitations.SingleAsync();
        committed.Accept(dietologist.Id);
        await committedContext.SaveChangesAsync();
        Assert.Equal(DietologistInvitationStatus.Accepted,
            (await database.DietologistInvitations.AsNoTracking().SingleAsync()).Status);
        Assert.Equal(2, await database.AuditEntries.CountAsync());
        Assert.Single(await database.AuditEntries.Where(entry => entry.Action == "dietologist.invitation.accepted").ToListAsync());
    }

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
                item.WeightPoints.Single().WeightKg == firstWeight.WeightKg),
            () => Assert.Contains(result, item =>
                item.ClientUserId == second.Id.Value &&
                item.LastMealAtUtc == secondMeal.Date &&
                item.WeightPoints.Single().WeightKg == secondWeight.WeightKg));
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

    // Exercise the synchronous EF entrypoint deliberately; the rest of the test uses async saves.
    private static void SaveSynchronously(FoodDiaryDbContext context) => context.SaveChanges();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }
}
