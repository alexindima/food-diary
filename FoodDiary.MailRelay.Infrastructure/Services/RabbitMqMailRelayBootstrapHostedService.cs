namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayBootstrapHostedService(
    RabbitMqMailRelayBroker broker,
    ILogger<RabbitMqMailRelayBootstrapHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        if (!broker.IsEnabled) {
            return;
        }
        await broker.DeclareTopologyAsync(cancellationToken);
        logger.LogInformation("RabbitMQ topology bootstrap completed for MailRelay.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
