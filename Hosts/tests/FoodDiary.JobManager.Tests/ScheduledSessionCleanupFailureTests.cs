using FoodDiary.JobManager.Services;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupSessions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class ScheduledSessionCleanupFailureTests {
    [Theory]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("15 3 * * *", true)]
    public void SessionCleanup_RequiresANonBlankCronSchedule(string cron, bool valid) {
        Assert.Equal(valid, UserSessionCleanupOptions.HasValidConfiguration(new UserSessionCleanupOptions { Cron = cron }));
    }

    [Fact]
    public async Task SessionCleanup_DispatchFailureIsRecordedAndPropagated() {
        ISender sender = Substitute.For<ISender>();
        var failure = new InvalidOperationException("Synthetic cleanup failure");
        sender.Send(Arg.Any<CleanupSessionsCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(failure));
        using var tracker = new JobExecutionStateTracker();
        var job = new UserSessionCleanupJob(sender, Options.Create(new UserSessionCleanupOptions()), new JobExecutionObserver(TimeProvider.System, tracker));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute()));

        JobExecutionStateSnapshot state = Assert.IsType<JobExecutionStateSnapshot>(tracker.GetSnapshot("users.sessions_cleanup"));
        Assert.Multiple(
            () => Assert.Equal(1, state.ConsecutiveFailures),
            () => Assert.NotNull(state.LastFailedAtUtc),
            () => Assert.Null(state.LastSucceededAtUtc));
    }

    [Fact]
    public async Task SessionCleanup_CanceledBeforeDispatchDoesNotRecordFailureOrCallTheHandler() {
        ISender sender = Substitute.For<ISender>();
        using var tracker = new JobExecutionStateTracker();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var job = new UserSessionCleanupJob(sender, Options.Create(new UserSessionCleanupOptions()), new JobExecutionObserver(TimeProvider.System, tracker));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellation.Token));

        Assert.Empty(sender.ReceivedCalls());
        JobExecutionStateSnapshot state = Assert.IsType<JobExecutionStateSnapshot>(tracker.GetSnapshot("users.sessions_cleanup"));
        Assert.Multiple(
            () => Assert.Equal(0, state.ConsecutiveFailures),
            () => Assert.Null(state.LastFailedAtUtc),
            () => Assert.Null(state.LastSucceededAtUtc));
    }

    [Fact]
    public async Task CancellationEmail_CallerCancellationIsPropagatedWithoutRecordingAJobFailure() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SendCancellationConfirmationsCommand>(), cancellation.Token)
            .Returns(Task.FromCanceled<BillingCancellationEmailRunResult>(cancellation.Token));
        using var tracker = new JobExecutionStateTracker();
        var job = new BillingCancellationEmailJob(sender, new JobExecutionObserver(TimeProvider.System, tracker), NullLogger<BillingCancellationEmailJob>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellation.Token));

        JobExecutionStateSnapshot state = Assert.IsType<JobExecutionStateSnapshot>(tracker.GetSnapshot("billing.cancellation-email"));
        Assert.Multiple(
            () => Assert.Equal(0, state.ConsecutiveFailures),
            () => Assert.Null(state.LastFailedAtUtc),
            () => Assert.Null(state.LastSucceededAtUtc));
    }
}
