using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Services;

public sealed class UserLoginEventCleanupHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<UserLoginEventCleanupOptions> options,
    ILogger<UserLoginEventCleanupHostedService> logger)
    : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var settings = options.Value;
        if (!settings.Enabled) {
            logger.LogInformation("User login event cleanup hosted service is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(settings.PollIntervalHours));

        do {
            try {
                await DeleteExpiredLoginEventsAsync(settings, stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Failed to delete expired user login events.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DeleteExpiredLoginEventsAsync(
        UserLoginEventCleanupOptions settings,
        CancellationToken cancellationToken) {
        var cutoffUtc = DateTime.UtcNow.AddDays(-settings.RetentionDays);

        using var scope = serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IUserLoginEventRepository>();
        var totalDeletedCount = 0;
        int deletedCount;
        do {
            deletedCount = await repository.DeleteOlderThanAsync(
                cutoffUtc,
                settings.BatchSize,
                cancellationToken);
            totalDeletedCount += deletedCount;
        } while (deletedCount == settings.BatchSize);

        if (totalDeletedCount > 0) {
            logger.LogInformation(
                "Deleted {DeletedCount} user login events older than {CutoffUtc}.",
                totalDeletedCount,
                cutoffUtc);
        }
    }
}
