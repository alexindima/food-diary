using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.JobManager.Services;
using NSubstitute;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class AtomicCommandReceiptCleanupJobTests {
    private const string JobName = "atomic-command-receipts.cleanup";
    private readonly IAtomicCommandReceiptMaintenance _maintenance = Substitute.For<IAtomicCommandReceiptMaintenance>();
    private readonly IJobExecutionStateTracker _tracker = Substitute.For<IJobExecutionStateTracker>();

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public async Task Execute_StopsAfterTheFirstIncompleteBatch(int lastCount) {
        using var cancellation = new CancellationTokenSource();
        _maintenance.DeleteExpiredBatchAsync(1000, cancellation.Token).Returns(1000, 1000, lastCount);

        await CreateJob().Execute(cancellation.Token);

        await _maintenance.Received(3).DeleteExpiredBatchAsync(1000, cancellation.Token);
        _tracker.Received(1).RecordStarted(JobName, Arg.Any<DateTime>());
        _tracker.Received(1).RecordSuccess(JobName, Arg.Any<DateTime>());
        _tracker.DidNotReceive().RecordFailure(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Execute_BoundsAContinuouslyFullQueueToOneHundredBatches() {
        _maintenance.DeleteExpiredBatchAsync(1000, CancellationToken.None).Returns(1000);

        await CreateJob().Execute();

        await _maintenance.Received(100).DeleteExpiredBatchAsync(1000, CancellationToken.None);
        _tracker.Received(1).RecordSuccess(JobName, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Execute_RecordsFailureAndPropagatesTheOriginalException() {
        var exception = new InvalidOperationException("Receipt storage unavailable.");
        _maintenance.DeleteExpiredBatchAsync(1000, CancellationToken.None).Returns(Task.FromException<int>(exception));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().Execute());

        Assert.Same(exception, actual);
        _tracker.Received(1).RecordFailure(JobName, Arg.Any<DateTime>());
        _tracker.DidNotReceive().RecordSuccess(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Execute_CallerCancellationDoesNotRecordAStorageFailure() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _maintenance.DeleteExpiredBatchAsync(1000, cancellation.Token).Returns(Task.FromCanceled<int>(cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateJob().Execute(cancellation.Token));

        await _maintenance.Received(1).DeleteExpiredBatchAsync(1000, cancellation.Token);
        _tracker.DidNotReceive().RecordFailure(Arg.Any<string>(), Arg.Any<DateTime>());
        _tracker.DidNotReceive().RecordSuccess(Arg.Any<string>(), Arg.Any<DateTime>());
    }

    private AtomicCommandReceiptCleanupJob CreateJob() => new(_maintenance, new JobExecutionObserver(TimeProvider.System, _tracker));
}
