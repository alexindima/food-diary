using FoodDiary.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupJob(
    IUserCleanupService cleanupService,
    IOptions<UserCleanupOptions> options,
    IDateTimeProvider dateTimeProvider,
    ILogger<UserCleanupJob> logger) {
    public async Task Execute(CancellationToken cancellationToken = default) {
        var settings = options.Value;
        var retentionDays = settings.RetentionDays > 0 ? settings.RetentionDays : 30;
        var batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        var olderThanUtc = dateTimeProvider.UtcNow.AddDays(-retentionDays);
        var totalDeleted = 0;

        Guid? reassignUserId = null;
        if (!string.IsNullOrWhiteSpace(settings.ReassignUserId)
            && Guid.TryParse(settings.ReassignUserId, out var parsed)) {
            reassignUserId = parsed;
        }

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
    }
}
