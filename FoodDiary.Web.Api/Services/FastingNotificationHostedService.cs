using FoodDiary.Application.Fasting.Services;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class FastingNotificationHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<FastingNotificationOptions> options,
    ILogger<FastingNotificationHostedService> logger)
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var settings = options.Value;
        if (!settings.Enabled) {
            logger.LogInformation("Fasting notification hosted service is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.PollIntervalSeconds));

        do {
            try {
                using var scope = serviceScopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IFastingNotificationScheduler>();
                await scheduler.ProcessDueNotificationsAsync(stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Failed to process due fasting notifications.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
