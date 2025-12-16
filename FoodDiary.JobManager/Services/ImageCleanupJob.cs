using FoodDiary.Application.Common.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class ImageCleanupJob(
    IImageAssetCleanupService cleanupService,
    IOptions<ImageCleanupOptions> options,
    ILogger<ImageCleanupJob> logger)
{
    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var olderThanUtc = DateTime.UtcNow.AddHours(-Math.Abs(settings.OlderThanHours));
        var totalDeleted = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var deleted = await cleanupService.CleanupOrphansAsync(olderThanUtc, settings.BatchSize, cancellationToken);
            totalDeleted += deleted;

            if (deleted < settings.BatchSize)
            {
                break;
            }
        }

        if (totalDeleted > 0)
        {
            logger.LogInformation("Removed {Count} orphaned image assets older than {OlderThan}", totalDeleted, olderThanUtc);
        }
    }
}

