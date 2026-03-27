using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Images.Common;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class ImageCleanupJob(
    IImageAssetCleanupService cleanupService,
    IOptions<ImageCleanupOptions> options,
    IDateTimeProvider dateTimeProvider,
    ILogger<ImageCleanupJob> logger) {
    public async Task Execute(CancellationToken cancellationToken = default) {
        var settings = options.Value;
        var olderThanHours = settings.OlderThanHours > 0 ? settings.OlderThanHours : 12;
        var batchSize = settings.BatchSize > 0 ? settings.BatchSize : 1;
        var olderThanUtc = dateTimeProvider.UtcNow.AddHours(-olderThanHours);
        var totalDeleted = 0;

        while (!cancellationToken.IsCancellationRequested) {
            var deleted = await cleanupService.CleanupOrphansAsync(olderThanUtc, batchSize, cancellationToken);
            totalDeleted += deleted;

            if (deleted < batchSize) {
                break;
            }
        }

        if (totalDeleted > 0) {
            logger.LogInformation("Removed {Count} orphaned image assets older than {OlderThan}", totalDeleted, olderThanUtc);
        }
    }
}
