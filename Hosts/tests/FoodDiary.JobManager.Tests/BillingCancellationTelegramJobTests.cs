using FoodDiary.JobManager.Services;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.SendTelegramCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingCancellationTelegramJobTests {
    [Fact]
    public async Task Job_DispatchesIndependentBoundedBatchAndRecordsSuccess() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SendTelegramCancellationConfirmationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BillingCancellationTelegramRunResult(1, 0, 0));
        using var tracker = new JobExecutionStateTracker();
        var job = new BillingCancellationTelegramJob(sender, new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingCancellationTelegramJob>.Instance);
        await job.Execute(CancellationToken.None);
        await sender.Received(1).Send(Arg.Is<SendTelegramCancellationConfirmationsCommand>(command => command.BatchSize == 100), CancellationToken.None);
        Assert.Single(sender.ReceivedCalls());
        Assert.Equal(0, tracker.GetSnapshot("billing.cancellation-telegram")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task Job_RecordsAndPropagatesFailure() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SendTelegramCancellationConfirmationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BillingCancellationTelegramRunResult>(new InvalidOperationException("Synthetic dispatch failure")));
        using var tracker = new JobExecutionStateTracker();
        var job = new BillingCancellationTelegramJob(sender, new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingCancellationTelegramJob>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(CancellationToken.None));
        Assert.Equal(1, tracker.GetSnapshot("billing.cancellation-telegram")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task Job_CallerCancellationDoesNotRecordFailure() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SendTelegramCancellationConfirmationsCommand>(), cancellation.Token)
            .Returns(Task.FromCanceled<BillingCancellationTelegramRunResult>(cancellation.Token));
        using var tracker = new JobExecutionStateTracker();
        var job = new BillingCancellationTelegramJob(sender, new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingCancellationTelegramJob>.Instance);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.Execute(cancellation.Token));
        Assert.Equal(0, tracker.GetSnapshot("billing.cancellation-telegram")?.ConsecutiveFailures);
        Assert.Null(tracker.GetSnapshot("billing.cancellation-telegram")?.LastSucceededAtUtc);
    }
}
