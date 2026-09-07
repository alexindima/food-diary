namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayDispatchNotifier(
    RabbitMqMailRelayBroker broker,
    ILogger<RabbitMqMailRelayDispatchNotifier> logger) : IMailRelayDispatchNotifier {
    public async Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken) {
        if (!broker.IsEnabled) {
            return;
        }
        logger.LogDebug("Queued relay email {QueuedEmailId} is ready for outbox publication to RabbitMQ.", queuedEmailId);
        await Task.CompletedTask.ConfigureAwait(false);
    }
}
