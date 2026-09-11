using FoodDiary.Application.Abstractions.Ai.Common;

namespace FoodDiary.JobManager.Services;

public sealed class FoodRecognitionWorker(IServiceScopeFactory scopes, ILogger<FoodRecognitionWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using IServiceScope scope = scopes.CreateScope();
                bool processed = await scope.ServiceProvider.GetRequiredService<IFoodRecognitionProcessor>()
                    .ProcessNextAsync(stoppingToken).ConfigureAwait(false);
                if (processed) {
                    continue;
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Food recognition worker failed. Claimed tasks are not redispatched after an uncertain provider outcome.");
            }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken).ConfigureAwait(false);
        }
    }
}
