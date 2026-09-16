using FoodDiary.Mediator;
using FoodDiary.Modules.Gamification.Contracts.Commands.ReconcileAchievements;
using FoodDiary.Modules.Gamification.PersistenceModel;
using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Modules.Gamification.Contracts.Models;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.PersistenceModel.Achievements;
using FoodDiary.Modules.Gamification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Gamification.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class AchievementPersistenceTests {
    [Fact]
    public async Task Enqueue_InMemoryCoalescesRequestsAndStagesUntilSaveAsync() {
        await using var context = new GamificationDbContext(new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var userId = UserId.New();
        var outbox = new AchievementEvaluationOutbox(context, context.AchievementEvaluationOutbox, TimeProvider.System);
        await outbox.EnqueueAsync(userId);
        Assert.Equal(EntityState.Added, Assert.Single(context.ChangeTracker.Entries<AchievementEvaluationOutboxMessage>()).State);
        await context.SaveChangesAsync();
        AchievementEvaluationOutboxMessage first = await context.AchievementEvaluationOutbox.SingleAsync();
        await outbox.EnqueueAsync(userId);
        await context.SaveChangesAsync();
        AchievementEvaluationOutboxMessage updated = await context.AchievementEvaluationOutbox.SingleAsync();
        Assert.Equal(first.Id, updated.Id);
        Assert.Equal(2, updated.Revision);
        Assert.Null(updated.ProcessedOnUtc);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Processor_WhenClaimIsRemovedOrTakenOver_DoesNotReleaseAnotherClaim(bool removed) {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        await using var context = new FoodDiaryDbContext(options);
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now);
        context.Add(message);
        await context.SaveChangesAsync();
        ISender handler = Substitute.For<ISender>();
        handler.Send(new ReconcileAchievementsCommand(message.UserId, message.CreatedOnUtc), Arg.Any<CancellationToken>()).Returns(async _ => {
            await using var writer = new FoodDiaryDbContext(options);
            AchievementEvaluationOutboxMessage current = await writer.AchievementEvaluationOutbox.SingleAsync();
            if (removed) {
                writer.Remove(current);
            } else {
                current.RequestEvaluation(Now.AddMinutes(1));
                current.MarkClaimed(Now.AddMinutes(5), "new-worker");
            }
            await writer.SaveChangesAsync();
        });
        var processor = new AchievementEvaluationOutboxProcessor(context, context.AchievementEvaluationOutbox, handler,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()), TimeProvider.System,
            NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        Assert.Equal(0, await processor.ProcessDueAsync(1));
        AchievementEvaluationOutboxMessage? remaining = await context.AchievementEvaluationOutbox.AsNoTracking().SingleOrDefaultAsync();
        if (removed) {
            Assert.Null(remaining);
        } else {
            Assert.NotNull(remaining);
            Assert.Equal("new-worker", remaining.LockedBy);
            Assert.Null(remaining.ProcessedOnUtc);
        }
    }
    private static readonly DateTime Now = new(2026, 8, 11, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void EvaluationRevision_PreservesClaimUntilExplicitRelease_AndUsesModuleModel() {
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now);
        message.MarkClaimed(Now.AddMinutes(5), "worker");

        message.RequestEvaluation(Now.AddMinutes(1));

        Assert.Multiple(
            () => Assert.Same(typeof(GamificationPersistenceModelRegistration).Assembly, message.GetType().Assembly),
            () => Assert.Equal(2, message.Revision),
            () => Assert.Equal(Now.AddMinutes(1), message.NextAttemptOnUtc),
            () => Assert.Equal("worker", message.LockedBy),
            () => Assert.Equal(Now.AddMinutes(5), message.LockedUntilUtc),
            () => Assert.Null(message.ProcessedOnUtc));

        message.ReleaseForUpdatedRevision();

        Assert.Multiple(
            () => Assert.Null(message.LockedBy),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Equal(2, message.Revision),
            () => Assert.Null(message.ProcessedOnUtc));
    }

    [Fact]
    public void AchievementEvaluationMessage_WithEmptyUserId_Throws() => Assert.Throws<ArgumentException>(() =>
        AchievementEvaluationOutboxMessage.Create(UserId.Empty, Now));

    [Fact]
    public void AchievementEvaluationMessage_Lifecycle_NormalizesAndClearsState() {
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now.ToLocalTime());

        message.MarkClaimed(Now.AddMinutes(1).ToLocalTime(), $"  {new string('w', 140)}  ");
        Assert.Multiple(
            () => Assert.Equal(DateTimeKind.Utc, message.CreatedOnUtc.Kind),
            () => Assert.Equal(128, message.LockedBy?.Length));

        message.MarkFailed($"  {new string('x', 2100)}  ", Now.AddMinutes(2).ToLocalTime());
        Assert.Multiple(
            () => Assert.Equal(1, message.AttemptCount),
            () => Assert.Equal(2048, message.LastError?.Length),
            () => Assert.Null(message.LockedBy));

        message.MarkDeadLettered(" ", Now.AddMinutes(3));
        Assert.Multiple(
            () => Assert.Equal(2, message.AttemptCount),
            () => Assert.NotNull(message.DeadLetteredOnUtc),
            () => Assert.Null(message.LastError));

        message.MarkReplayed(Now.AddMinutes(4));
        Assert.Null(message.DeadLetteredOnUtc);
        message.MarkProcessed(Now.AddMinutes(5));
        Assert.Multiple(
            () => Assert.NotNull(message.ProcessedOnUtc),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.LastError));
    }

    [Fact]
    public async Task AchievementEvaluationOutboxProcessor_ProcessesDueMessage() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now);
        context.AchievementEvaluationOutbox.Add(message);
        await context.SaveChangesAsync();
        ISender handler = Substitute.For<ISender>();
        var processor = new AchievementEvaluationOutboxProcessor(
            context, context.AchievementEvaluationOutbox,
            handler,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10);

        Assert.Equal(1, processed);
        await handler.Received(1).Send(new ReconcileAchievementsCommand(message.UserId, message.CreatedOnUtc), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AchievementEvaluationOutboxProcessor_WhenReconciliationFails_RecordsRetryForUser() {
        await using FoodDiaryDbContext context = CreateContext();
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now);
        context.AchievementEvaluationOutbox.Add(message);
        await context.SaveChangesAsync();
        ISender handler = Substitute.For<ISender>();
        handler.Send(new ReconcileAchievementsCommand(message.UserId, message.CreatedOnUtc), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Simulated reconciliation failure.")));
        var processor = new AchievementEvaluationOutboxProcessor(
            context, context.AchievementEvaluationOutbox,
            handler,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 10);

        AchievementEvaluationOutboxMessage persisted = await context.AchievementEvaluationOutbox.AsNoTracking().SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(1, persisted.AttemptCount),
            () => Assert.Equal("Outbox dispatch failed (InvalidOperationException).", persisted.LastError));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(9, true)]
    public async Task AchievementEvaluationOutboxProcessor_WhenEvaluationIsRequestedDuringDispatch_ReleasesClaimWithoutMarkingProcessed(int previousAttempts, bool failDispatch) {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new FoodDiaryDbContext(options);
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), Now);
        for (int attempt = 0; attempt < previousAttempts; attempt++) {
            message.MarkFailed("Previous failure", Now);
        }
        context.AchievementEvaluationOutbox.Add(message);
        await context.SaveChangesAsync();
        ISender handler = Substitute.For<ISender>();
        handler.Send(new ReconcileAchievementsCommand(message.UserId, message.CreatedOnUtc), Arg.Any<CancellationToken>())
            .Returns(async _ => {
                await using var writer = new FoodDiaryDbContext(options);
                AchievementEvaluationOutboxMessage updated = await writer.AchievementEvaluationOutbox.SingleAsync();
                updated.RequestEvaluation(Now.AddMinutes(1));
                await writer.SaveChangesAsync();
                if (failDispatch) {
                    throw new InvalidOperationException("Old revision failed.");
                }
            });
        var processor = new AchievementEvaluationOutboxProcessor(
            context, context.AchievementEvaluationOutbox,
            handler,
            Microsoft.Extensions.Options.Options.Create(new OutboxProcessingOptions()),
            TimeProvider.System,
            NullLogger<AchievementEvaluationOutboxProcessor>.Instance);

        int processed = await processor.ProcessDueAsync(batchSize: 1);
        AchievementEvaluationOutboxMessage pending = await context.AchievementEvaluationOutbox.AsNoTracking().SingleAsync();

        Assert.Multiple(
            () => Assert.Equal(0, processed),
            () => Assert.Equal(2, pending.Revision),
            () => Assert.Equal(0, pending.AttemptCount),
            () => Assert.Null(pending.DeadLetteredOnUtc),
            () => Assert.Null(pending.LastError),
            () => Assert.Equal(Now.AddMinutes(1), pending.NextAttemptOnUtc),
            () => Assert.Null(pending.ProcessedOnUtc),
            () => Assert.Null(pending.LockedBy));
    }

    [Fact]
    public async Task AchievementDefinitionStore_GetAllAsync_OrdersDefinitions() {
        await using FoodDiaryDbContext context = CreateContext();
        AchievementDefinition second = CreateDefinition("second", sortOrder: 2);
        AchievementDefinition firstB = CreateDefinition("b", sortOrder: 1);
        AchievementDefinition firstA = CreateDefinition("a", sortOrder: 1);
        context.AchievementDefinitions.AddRange(second, firstB, firstA);
        await context.SaveChangesAsync();

        IReadOnlyList<AchievementDefinition> result = await new AchievementDefinitionStore(context, context.AchievementDefinitions, context.UserAchievements).GetAllAsync();

        Assert.Equal(["a", "b", "second"], result.Select(static item => item.Key), StringComparer.Ordinal);
        context.ChangeTracker.Clear();
        IReadOnlyList<AchievementDefinitionAdminModel> models = await new AchievementDefinitionStore(context, context.AchievementDefinitions, context.UserAchievements).GetForAdministrationAsync();
        Assert.Multiple(
            () => Assert.Equal(["a", "b", "second"], models.Select(static item => item.Key), StringComparer.Ordinal),
            () => Assert.All(models, item => Assert.Equal(0, item.AwardedUsers)),
            () => Assert.Empty(context.ChangeTracker.Entries()));
    }

    private static AchievementDefinition CreateDefinition(string key, int sortOrder) => AchievementDefinition.Create(
        key, "habits", AchievementMetric.TotalMeals, 10, "Title RU", "Title", "Description RU", "Description", "trophy", sortOrder);

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FoodDiaryDbContext(options);
    }
}
