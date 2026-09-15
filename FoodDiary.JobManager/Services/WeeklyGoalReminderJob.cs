using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.WeeklyGoals.Contracts.Commands.SendWeeklyGoalReminders;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class WeeklyGoalReminderJob(
    ISender sender,
    IOptions<WeeklyGoalReminderOptions> options,
    JobExecutionObserver observer,
    ILogger<WeeklyGoalReminderJob> logger) {
    private const string JobName = "weekly_goals.reminders";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        try {
            if (!options.Value.Enabled) {
                observer.RecordSuccess(JobName, processed: 0);
                return;
            }

            int processed = await sender.Send(new SendWeeklyGoalRemindersCommand(), cancellationToken).ConfigureAwait(false);
            observer.RecordSuccess(JobName, processed: processed);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("Weekly goal reminder job was canceled.");
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "Weekly goal reminder job failed.");
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }
}
