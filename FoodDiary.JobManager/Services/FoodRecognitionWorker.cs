using FoodDiary.Mediator;
using FoodDiary.Modules.Ai.Contracts.Commands.ProcessNextFoodRecognition;

namespace FoodDiary.JobManager.Services;

public sealed class FoodRecognitionWorker(IServiceScopeFactory scopes, ILogger<FoodRecognitionWorker> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                using IServiceScope scope = scopes.CreateScope();
                bool processed = await scope.ServiceProvider.GetRequiredService<ISender>()
                    .Send(new ProcessNextFoodRecognitionCommand(), stoppingToken).ConfigureAwait(false);
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
