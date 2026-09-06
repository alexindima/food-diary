using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Achievements;
using FoodDiary.Modules.Gamification.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AchievementLeaseOwnershipIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Completion_WhenClaimWasReplaced_DoesNotCompleteOrReleaseNewOwner(bool updateRevision, bool failDispatch) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        var user = User.Create("achievement-lease@example.com", "hash");
        var message = AchievementEvaluationOutboxMessage.Create(user.Id, now);
        seed.AddRange(user, message);
        await seed.SaveChangesAsync();
        await using FoodDiaryDbContext worker = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!, enableRetries: true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(worker);
        services.AddSingleton(TimeProvider.System);
        services.AddGamificationModule();
        services.AddSingleton<IAchievementReconciliationHandler>(new ReplaceClaimHandler(seed, updateRevision, failDispatch));
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAchievementEvaluationOutboxProcessor processor = provider.GetRequiredService<IAchievementEvaluationOutboxProcessor>();
        Assert.Equal(0, await processor.ProcessDueAsync(1));
        seed.ChangeTracker.Clear();
        AchievementEvaluationOutboxMessage persisted = await seed.AchievementEvaluationOutbox.SingleAsync();
        Assert.Multiple(
            () => Assert.Equal("replacement-worker", persisted.LockedBy, StringComparer.Ordinal),
            () => Assert.NotNull(persisted.LockedUntilUtc),
            () => Assert.Null(persisted.ProcessedOnUtc),
            () => Assert.Equal(updateRevision ? 2 : 1, persisted.Revision));
    }

    [RequiresDockerTheory]
    [InlineData(0, true)]
    [InlineData(9, true)]
    [InlineData(9, false)]
    public async Task Finalization_WhenNewEvaluationArrives_PreservesNewRevision(int previousAttempts, bool failDispatch) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow.AddMinutes(-1);
        var user = User.Create("achievement-revision@example.com", "hash");
        var message = AchievementEvaluationOutboxMessage.Create(user.Id, now);
        for (int attempt = 0; attempt < previousAttempts; attempt++) {
            message.MarkFailed("Previous failure", now);
        }
        seed.AddRange(user, message);
        await seed.SaveChangesAsync();
        await using FoodDiaryDbContext worker = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!, enableRetries: true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(worker);
        services.AddSingleton(TimeProvider.System);
        services.AddGamificationModule();
        services.AddSingleton<IAchievementReconciliationHandler>(new RequestNewRevisionHandler(seed, failDispatch));
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAchievementEvaluationOutboxProcessor processor = provider.GetRequiredService<IAchievementEvaluationOutboxProcessor>();

        Assert.Equal(0, await processor.ProcessDueAsync(1));

        seed.ChangeTracker.Clear();
        AchievementEvaluationOutboxMessage pending = await seed.AchievementEvaluationOutbox.SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(2, pending.Revision),
            () => Assert.Equal(0, pending.AttemptCount),
            () => Assert.Null(pending.DeadLetteredOnUtc),
            () => Assert.Null(pending.ProcessedOnUtc),
            () => Assert.Null(pending.LastError),
            () => Assert.Null(pending.LockedBy),
            () => Assert.Null(pending.LockedUntilUtc),
            () => Assert.Equal(pending.CreatedOnUtc, pending.NextAttemptOnUtc),
            () => Assert.True(pending.CreatedOnUtc > now));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RequestNewRevisionHandler(FoodDiaryDbContext context, bool failDispatch) : IAchievementReconciliationHandler {
        public async Task ReconcileAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken = default) {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(context);
            services.AddSingleton(TimeProvider.System);
            services.AddGamificationModule();
            await using ServiceProvider provider = services.BuildServiceProvider();
            await provider.GetRequiredService<IAchievementEvaluationOutbox>().EnqueueAsync(userId, cancellationToken);
            if (failDispatch) {
                throw new InvalidOperationException("Old revision failed after a new request.");
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReplaceClaimHandler(FoodDiaryDbContext context, bool updateRevision, bool failDispatch) : IAchievementReconciliationHandler {
        public async Task ReconcileAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken = default) {
            context.ChangeTracker.Clear();
            AchievementEvaluationOutboxMessage message = await context.AchievementEvaluationOutbox.SingleAsync(cancellationToken);
            if (updateRevision) {
                message.RequestEvaluation(occurredAtUtc.AddMinutes(1));
            }
            message.MarkClaimed(occurredAtUtc.AddMinutes(10), "replacement-worker");
            await context.SaveChangesAsync(cancellationToken);
            if (failDispatch) {
                throw new InvalidOperationException("Old worker failed after losing its claim.");
            }
        }
    }
}
