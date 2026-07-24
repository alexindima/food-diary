using System.Diagnostics;
using FoodDiary.Application.Dietologist.Services;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class ClientTaskReminderJob(
    ClientTaskDueReminderProcessor processor,
    IOptions<ClientTaskReminderOptions> options,
    JobExecutionObserver observer,
    ILogger<ClientTaskReminderJob> logger) {
    private const string JobName = "dietologist.client_task_reminders";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            if (!options.Value.Enabled) {
                observer.RecordSuccess(JobName, processed: 0);
                return;
            }

            int processed = await processor.ProcessAsync(cancellationToken).ConfigureAwait(false);
            observer.RecordSuccess(JobName, processed: processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Client task reminder job was canceled.");
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Client task reminder job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
