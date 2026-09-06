using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class OutboxLeaseOwnershipIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredOwner_CannotFinalizeOrReleaseAnotherWorkersClaim(bool failDispatch) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var clock = new LeaseClock(DateTimeOffset.UtcNow);
        var message = ImageObjectDeletionOutboxMessage.Create("lease-ownership-test", clock.GetUtcNow().UtcDateTime);
        seed.Add(message);
        await seed.SaveChangesAsync();
        seed.ChangeTracker.Clear();
        string connection = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext first = databaseFixture.CreateDbContext(connection, enableRetries: true);
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(connection, enableRetries: true);
        Assert.Single(await first.ImageObjectDeletionOutbox.ToListAsync());
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string? firstOwner = null;
        string? secondOwner = null;
        Task<int> firstRun = RunAsync(first, clock, async (claimed, token) => {
            firstOwner = claimed.LockedBy;
            firstEntered.SetResult();
            await firstRelease.Task.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System, token);
            if (failDispatch) {
                throw new InvalidOperationException("Expired worker failure");
            }
        });
        Task<int>? secondRun = null;
        try {
            await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System);
            clock.Advance(TimeSpan.FromMinutes(6));
            secondRun = RunAsync(second, clock, async (claimed, token) => {
                secondOwner = claimed.LockedBy;
                secondEntered.SetResult();
                await secondRelease.Task.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System, token);
            });
            await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System);
            firstRelease.SetResult();
            Assert.Equal(0, await firstRun.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System));
            ImageObjectDeletionOutboxMessage during = await seed.ImageObjectDeletionOutbox.AsNoTracking().SingleAsync();
            Assert.Multiple(
                () => Assert.NotEqual(firstOwner, secondOwner, StringComparer.Ordinal),
                () => Assert.Equal(secondOwner, during.LockedBy, StringComparer.Ordinal),
                () => Assert.NotNull(during.LockedUntilUtc),
                () => Assert.Null(during.ProcessedOnUtc),
                () => Assert.Null(during.DeadLetteredOnUtc),
                () => Assert.Equal(0, during.AttemptCount),
                () => Assert.False(secondRun.IsCompleted));
            secondRelease.SetResult();
            Assert.Equal(1, await secondRun.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System));
            ImageObjectDeletionOutboxMessage completed = await seed.ImageObjectDeletionOutbox.AsNoTracking().SingleAsync();
            Assert.Multiple(
                () => Assert.NotNull(completed.ProcessedOnUtc),
                () => Assert.Null(completed.LockedBy),
                () => Assert.Equal(0, completed.AttemptCount));
        } finally {
            firstRelease.TrySetResult();
            secondRelease.TrySetResult();
            await firstRun.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System);
            if (secondRun is not null) {
                await secondRun.WaitAsync(TimeSpan.FromSeconds(20), TimeProvider.System);
            }
        }
    }

    private static Task<int> RunAsync(FoodDiaryDbContext context, TimeProvider clock,
        Func<ImageObjectDeletionOutboxMessage, CancellationToken, Task> dispatchAsync) =>
        OutboxProcessingEngine.ProcessDueAsync(context, context.ImageObjectDeletionOutbox,
            "\"ImageObjectDeletionOutbox\"", "lease_test", 1, new OutboxProcessingOptions(), clock,
            dispatchAsync, message => message.Id, NullLogger.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class LeaseClock(DateTimeOffset now) : TimeProvider {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }
}
