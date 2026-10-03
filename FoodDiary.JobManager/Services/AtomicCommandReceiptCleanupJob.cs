using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using Hangfire;
using System.Diagnostics;

namespace FoodDiary.JobManager.Services;

public sealed class AtomicCommandReceiptCleanupJob(IAtomicCommandReceiptMaintenance maintenance, JobExecutionObserver observer) {
    private const string JobName = "atomic-command-receipts.cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        int deleted = 0;
        try {
            for (int batch = 0; batch < 100; batch++) {
                int count = await maintenance.DeleteExpiredBatchAsync(1000, cancellationToken).ConfigureAwait(false);
                deleted += count;
                if (count < 1000) {
                    break;
                }
            }
            observer.RecordSuccess(JobName, deleted: deleted);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            observer.RecordCanceled(JobName);
            throw;
        } catch {
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
