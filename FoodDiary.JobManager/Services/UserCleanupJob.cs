using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Users.Common;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupJob(
    IUserCleanupService cleanupService,
    IOptions<UserCleanupOptions> options,
    IDateTimeProvider dateTimeProvider,
    ILogger<UserCleanupJob> logger) {
    public async Task Execute(CancellationToken cancellationToken = default) {
        var stopwatch = Stopwatch.StartNew();
        var settings = options.Value;
        var retentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 30;
        var batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        var olderThanUtc = dateTimeProvider.UtcNow.AddDays(-retentionDays);
        var totalDeleted = 0;
        const string jobName = "users.cleanup";

        Guid? reassignUserId = null;
        if (!string.IsNullOrWhiteSpace(settings.ReassignUserId)
            && Guid.TryParse(settings.ReassignUserId, out var parsed)) {
            reassignUserId = parsed;
        }

        try {
            while (!cancellationToken.IsCancellationRequested) {
                var deleted = await cleanupService.CleanupDeletedUsersAsync(
                    olderThanUtc,
                    batchSize,
                    reassignUserId,
                    cancellationToken);

                totalDeleted += deleted;

                if (deleted < batchSize) {
                    break;
                }
            }

            if (totalDeleted > 0) {
                logger.LogInformation("Removed {Count} users deleted before {OlderThan}", totalDeleted, olderThanUtc);
            }

            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "success"));
            JobManagerTelemetry.JobDeletedItemsCounter.Add(
                totalDeleted,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        } catch {
            JobManagerTelemetry.JobExecutionCounter.Add(
                1,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName),
                new KeyValuePair<string, object?>("fooddiary.job.outcome", "failure"));
            throw;
        } finally {
            stopwatch.Stop();
            JobManagerTelemetry.JobExecutionDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("fooddiary.job.name", jobName));
        }
    }
}
