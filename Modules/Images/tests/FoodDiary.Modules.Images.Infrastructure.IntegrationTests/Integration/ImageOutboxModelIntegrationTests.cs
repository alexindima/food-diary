using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Images;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ImageOutboxModelIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Lifecycle_PersistsConfirmationRetryAndReplayState() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var now = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var message = ImageObjectDeletionOutboxMessage.Create("images/pending.webp", isConfirmed: false, now);
        message.MarkClaimed(now.AddMinutes(1), "worker");
        context.ImageObjectDeletionOutbox.Add(message);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ImageObjectDeletionOutboxMessage claimed = await context.ImageObjectDeletionOutbox.SingleAsync();
        Assert.Multiple(
            () => Assert.False(claimed.IsConfirmed),
            () => Assert.Equal("worker", claimed.LockedBy),
            () => Assert.Equal(now.AddMinutes(1), claimed.LockedUntilUtc),
            () => Assert.Same(typeof(ImagesPersistenceModelBuilderExtensions).Assembly, claimed.GetType().Assembly));
        claimed.MarkFailed("retry", now.AddMinutes(2));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ImageObjectDeletionOutboxMessage failed = await context.ImageObjectDeletionOutbox.SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(1, failed.AttemptCount),
            () => Assert.Equal("retry", failed.LastError),
            () => Assert.Equal(now.AddMinutes(2), failed.NextAttemptOnUtc),
            () => Assert.Null(failed.LockedBy),
            () => Assert.Null(failed.LockedUntilUtc));
        failed.MarkDeadLettered("final failure", now.AddMinutes(3));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ImageObjectDeletionOutboxMessage deadLetter = await context.ImageObjectDeletionOutbox.SingleAsync();
        Assert.Equal(now.AddMinutes(3), deadLetter.DeadLetteredOnUtc);
        deadLetter.MarkReplayed(now.AddMinutes(4));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        ImageObjectDeletionOutboxMessage replay = await context.ImageObjectDeletionOutbox.SingleAsync();
        Assert.Multiple(
            () => Assert.Equal(2, replay.AttemptCount),
            () => Assert.Equal(now.AddMinutes(4), replay.NextAttemptOnUtc),
            () => Assert.Null(replay.DeadLetteredOnUtc),
            () => Assert.Null(replay.LastError),
            () => Assert.Null(replay.ProcessedOnUtc),
            () => Assert.False(replay.IsConfirmed));
    }
}
