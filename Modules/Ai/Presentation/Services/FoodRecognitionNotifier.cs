using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Presentation.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Services;

public sealed class FoodRecognitionNotifier(
    IServiceScopeFactory scopes,
    IHubContext<FoodRecognitionHub> hub,
    TimeProvider timeProvider,
    ILogger<FoodRecognitionNotifier> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        DateTime since = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(-5);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) {
            try {
                DateTime next = timeProvider.GetUtcNow().UtcDateTime;
                using IServiceScope scope = scopes.CreateScope();
                IReadOnlyList<FoodRecognitionJobUpdate> updates = await scope.ServiceProvider.GetRequiredService<IFoodRecognitionJobStore>()
                    .GetUpdatesAsync(since, stoppingToken).ConfigureAwait(false);
                foreach (FoodRecognitionJobUpdate update in updates) {
                    // Send only an invalidation hint. The owner-scoped HTTP endpoint is authoritative.
                    await hub.Clients.User(update.UserId.ToString())
                        .SendAsync("RecognitionChanged", update.Id, stoppingToken).ConfigureAwait(false);
                }
                since = next.AddSeconds(-2);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogWarning(ex, "Food recognition notification delivery failed; clients recover through HTTP.");
            }
        }
    }
}
