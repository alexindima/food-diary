using FoodDiary.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class UserCleanupJob(
    IUserCleanupService cleanupService,
    IOptions<UserCleanupOptions> options,
    ILogger<UserCleanupJob> logger)
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var olderThanUtc = DateTime.UtcNow.AddDays(-Math.Abs(settings.RetentionDays));
        var totalDeleted = 0;

        Guid? reassignUserId = null;
        if (!string.IsNullOrWhiteSpace(settings.ReassignUserId)
            && Guid.TryParse(settings.ReassignUserId, out var parsed))
        {
            reassignUserId = parsed;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            var deleted = await cleanupService.CleanupDeletedUsersAsync(
                olderThanUtc,
                settings.BatchSize,
                reassignUserId,
                cancellationToken);

            totalDeleted += deleted;

            if (deleted < settings.BatchSize)
            {
                break;
            }
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation("Removed {Count} users deleted before {OlderThan}", totalDeleted, olderThanUtc);
        }
    }
}
