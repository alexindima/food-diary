namespace FoodDiary.MailRelay.Services;

public sealed class MailRelaySchemaInitializerHostedService(
    MailRelayQueueStore queueStore,
    ILogger<MailRelaySchemaInitializerHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        await queueStore.EnsureSchemaAsync(cancellationToken);
        logger.LogInformation("Mail relay queue schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
