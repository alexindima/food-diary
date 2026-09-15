using System.Diagnostics;
using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Commands.CleanupDeletedUsers;
using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupJob(
    ISender sender,
    IOptions<UserCleanupOptions> options,
    JobExecutionObserver observer,
    ILogger<UserCleanupJob> logger) {
    private const string JobName = "users.cleanup";

    [AutomaticRetry(Attempts = RecurringJobExecutionPolicy.CleanupRetryAttempts, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    [DisableConcurrentExecution(RecurringJobExecutionPolicy.CleanupConcurrencyTimeoutSeconds)]
    public async Task Execute(CancellationToken cancellationToken = default) {
        Stopwatch stopwatch = observer.Start(JobName);
        UserCleanupOptions settings = options.Value;
        int retentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 30;
        int batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        DateTime olderThanUtc = observer.UtcNow.AddDays(-retentionDays);
        Guid? reassignUserId = ParseReassignUserId(settings);
        int totalDeleted = 0;

        try {
            cancellationToken.ThrowIfCancellationRequested();
            totalDeleted = await sender.Send(new CleanupDeletedUsersCommand(olderThanUtc, batchSize, reassignUserId), cancellationToken).ConfigureAwait(false);

            if (totalDeleted > 0) {
                logger.LogInformation("Removed {Count} users deleted before {OlderThan}", totalDeleted, olderThanUtc);
            }

            observer.RecordSuccess(JobName, deleted: totalDeleted);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            logger.LogInformation("User cleanup job was canceled after processing {DeletedCount} users.", totalDeleted);
            observer.RecordCanceled(JobName);
            throw;
        } catch (Exception ex) {
            logger.LogError(ex, "User cleanup job failed after processing {DeletedCount} users so far.", totalDeleted);
            observer.RecordFailure(JobName);
            throw;
        } finally {
            JobExecutionObserver.RecordDuration(JobName, stopwatch);
        }
    }

    private static Guid? ParseReassignUserId(UserCleanupOptions settings) {
        return !string.IsNullOrWhiteSpace(settings.ReassignUserId)
            && Guid.TryParse(settings.ReassignUserId, out Guid parsed)
            ? parsed
            : null;
    }
}
