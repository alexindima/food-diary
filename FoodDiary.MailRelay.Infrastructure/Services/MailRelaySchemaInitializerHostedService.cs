namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class MailRelaySchemaInitializerHostedService(
    IMailRelaySchemaInitializer schemaInitializer,
    ILogger<MailRelaySchemaInitializerHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        await schemaInitializer.EnsureSchemaAsync(cancellationToken);
        logger.LogInformation("Mail relay queue schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
