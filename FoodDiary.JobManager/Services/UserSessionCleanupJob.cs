using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Contracts.Authentication.Commands.CleanupSessions;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class UserSessionCleanupJob(ISender sender, IOptions<UserSessionCleanupOptions> options, JobExecutionObserver observer) {
    private const string JobName = "users.sessions_cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            cancellationToken.ThrowIfCancellationRequested();
            UserSessionCleanupOptions settings = options.Value;
            int deleted = settings.Enabled
                ? await sender.Send(new CleanupSessionsCommand(observer.UtcNow.AddDays(-settings.RetentionDays), settings.BatchSize, settings.MaxBatches), cancellationToken).ConfigureAwait(false)
                : 0;
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
