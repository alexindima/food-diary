using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class NotificationWebPushOutboxHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationWebPushOutboxOptions> options,
    ILogger<NotificationWebPushOutboxHostedService> logger)
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        NotificationWebPushOutboxOptions settings = options.Value;
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
        NotificationWebPushOutboxOptions settings,
        CancellationToken cancellationToken) {
        try {
            using IServiceScope scope = scopeFactory.CreateScope();
            INotificationWebPushOutboxProcessor processor = scope.ServiceProvider.GetRequiredService<INotificationWebPushOutboxProcessor>();
            int processed = await processor.ProcessDueAsync(settings.BatchSize, cancellationToken).ConfigureAwait(false);
            if (processed > 0) {
                logger.LogInformation("Processed {ProcessedCount} notification web-push outbox messages.", processed);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            logger.LogWarning(ex, "Notification web-push outbox processing failed.");
        }
    }
}
