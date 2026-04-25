namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayDispatchNotifier(
    RabbitMqMailRelayBroker broker,
    ILogger<RabbitMqMailRelayDispatchNotifier> logger) : IMailRelayDispatchNotifier {
    private readonly ILogger<RabbitMqMailRelayDispatchNotifier> _logger = logger;

    public async Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken) {
        if (!broker.IsEnabled) {
            return;
        }
        _logger.LogDebug("Queued relay email {QueuedEmailId} is ready for outbox publication to RabbitMQ.", queuedEmailId);
        await Task.CompletedTask;
    }
}
