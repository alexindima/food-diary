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
    [InlineData(false)]
    [InlineData(true)]
    public async Task Completion_WhenClaimWasReplaced_DoesNotCompleteOrReleaseNewOwner(bool updateRevision) {
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
        services.AddSingleton<IAchievementReconciliationHandler>(new ReplaceClaimHandler(seed, updateRevision));
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

    [ExcludeFromCodeCoverage]
    private sealed class ReplaceClaimHandler(FoodDiaryDbContext context, bool updateRevision) : IAchievementReconciliationHandler {
        public async Task ReconcileAsync(UserId userId, DateTime occurredAtUtc, CancellationToken cancellationToken = default) {
            context.ChangeTracker.Clear();
            AchievementEvaluationOutboxMessage message = await context.AchievementEvaluationOutbox.SingleAsync(cancellationToken);
            if (updateRevision) {
                message.RequestEvaluation(occurredAtUtc.AddMinutes(1));
            }
            message.MarkClaimed(occurredAtUtc.AddMinutes(10), "replacement-worker");
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
