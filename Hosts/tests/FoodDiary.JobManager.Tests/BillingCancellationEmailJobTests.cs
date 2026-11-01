using FoodDiary.JobManager.Services;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Commands.SendCancellationConfirmations;
using FoodDiary.Modules.Billing.Contracts.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class BillingCancellationEmailJobTests {
    [Fact]
    public async Task Job_DispatchesBoundedBatchAndRecordsSuccess() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SendCancellationConfirmationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(new BillingCancellationEmailRunResult(1, 0, 0));
        var tracker = new JobExecutionStateTracker();
        var job = new BillingCancellationEmailJob(sender, new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingCancellationEmailJob>.Instance);
        await job.Execute(CancellationToken.None);
        await sender.Received(1).Send(Arg.Is<SendCancellationConfirmationsCommand>(command => command.BatchSize == 100), CancellationToken.None);
        Assert.Equal(0, tracker.GetSnapshot("billing.cancellation-email")?.ConsecutiveFailures);
    }

    [Fact]
    public async Task Job_RecordsAndPropagatesFailure() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SendCancellationConfirmationsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BillingCancellationEmailRunResult>(new InvalidOperationException("Synthetic dispatch failure")));
        var tracker = new JobExecutionStateTracker();
        var job = new BillingCancellationEmailJob(sender, new JobExecutionObserver(TimeProvider.System, tracker),
            NullLogger<BillingCancellationEmailJob>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.Execute(CancellationToken.None));
        Assert.Equal(1, tracker.GetSnapshot("billing.cancellation-email")?.ConsecutiveFailures);
    }
}
