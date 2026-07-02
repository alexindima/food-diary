using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class ImageObjectDeletionOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<ImageObjectDeletionOutboxOptions> options,
    ILogger<ImageObjectDeletionOutboxHostedService> logger)
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        ImageObjectDeletionOutboxOptions settings = options.Value;
        if (!settings.Enabled) {
            return;
        }

        await ProcessOnceAsync(settings, stoppingToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) {
            await ProcessOnceAsync(settings, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessOnceAsync(
        ImageObjectDeletionOutboxOptions settings,
        CancellationToken cancellationToken) {
        try {
            using IServiceScope scope = scopeFactory.CreateScope();
            IImageObjectDeletionOutboxProcessor processor = scope.ServiceProvider.GetRequiredService<IImageObjectDeletionOutboxProcessor>();
            int processed = await processor.ProcessDueAsync(settings.BatchSize, cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Processed {ProcessedCount} image object deletion outbox messages.", processed);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Image object deletion outbox processing failed.");
        }
    }
}
