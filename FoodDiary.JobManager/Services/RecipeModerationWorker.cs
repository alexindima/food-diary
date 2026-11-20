using FoodDiary.Modules.Recipes.Infrastructure.Moderation;

namespace FoodDiary.JobManager.Services;

public sealed class RecipeModerationWorker(IServiceScopeFactory scopes, ILogger<RecipeModerationWorker> logger) : BackgroundService {
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(RunAsync(observation: false, stoppingToken), RunAsync(observation: true, stoppingToken));

    private async Task RunAsync(bool observation, CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            try {
                await using AsyncServiceScope scope = scopes.CreateAsyncScope();
                IRecipeModerationProcessor processor = scope.ServiceProvider.GetRequiredService<IRecipeModerationProcessor>();
                if (await processor.ProcessNextAsync(observation, cancellationToken)) {
                    continue;
                }
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                logger.LogError("Recipe moderation worker iteration failed in observation={Observation} with {ExceptionType}.",
                    observation, exception.GetType().Name);
            }

            try {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                return;
            }
        }
    }
}
