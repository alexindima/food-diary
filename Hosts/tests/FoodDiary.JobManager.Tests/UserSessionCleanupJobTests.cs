using FoodDiary.JobManager.Services;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CleanupSessions;
using FoodDiary.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserSessionCleanupJobTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Execute_UsesPastRetentionCutoffAndHonorsDisabledState(bool enabled) {
        DateTime now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        IRefreshTokenSessionWriteRepository repository = Substitute.For<IRefreshTokenSessionWriteRepository>();
        using var tracker = new JobExecutionStateTracker();
        var job = new UserSessionCleanupJob(RequestTestSender.Create(new CleanupSessionsCommandHandler(repository)),
            Options.Create(new UserSessionCleanupOptions { Enabled = enabled, RetentionDays = 30, BatchSize = 25, MaxBatches = 3 }),
            new JobExecutionObserver(new FixedClock(now), tracker));
        await job.Execute();
        if (enabled) {
            await repository.Received(1).DeleteInactiveBeforeAsync(now.AddDays(-30), 25, CancellationToken.None);
        } else {
            Assert.Empty(repository.ReceivedCalls());
        }
    }

    [Theory]
    [InlineData(0, 1, 1, false)]
    [InlineData(30, 0, 1, false)]
    [InlineData(30, 1001, 1, false)]
    [InlineData(30, 1, 101, false)]
    [InlineData(30, 500, 20, true)]
    public void Options_RejectUnsafeRetentionOrUnboundedBatches(int retention, int batch, int maxBatches, bool expected) =>
        Assert.Equal(expected, UserSessionCleanupOptions.HasValidConfiguration(new UserSessionCleanupOptions {
            RetentionDays = retention, BatchSize = batch, MaxBatches = maxBatches,
        }));

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
