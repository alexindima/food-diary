using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class MailRelayOutboxPublisherHostedService(
    RabbitMqMailRelayBroker broker,
    IOptions<MailRelayBrokerOptions> brokerOptions,
    IMailRelayQueueStore queueStore,
    ILogger<MailRelayOutboxPublisherHostedService> logger) : BackgroundService {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal)) {
            logger.LogInformation("Mail relay outbox publisher is disabled because backend mode is {Backend}.", _brokerOptions.Backend);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        do {
            try {
                var batch = await queueStore.ClaimOutboxBatchAsync(stoppingToken);
                if (batch.Count == 0) {
                    continue;
                }

                foreach (var message in batch) {
                    try {
                        await broker.PublishOutboundAsync(message.EmailId, stoppingToken);
                        await queueStore.MarkOutboxPublishedAsync(message.Id, stoppingToken);
                    } catch (Exception ex) {
                        await queueStore.MarkOutboxFailedAsync(message.Id, message.AttemptCount, ex.ToString(), stoppingToken);
                        logger.LogWarning(ex, "Mail relay outbox publish failed for outbox message {OutboxMessageId}.", message.Id);
                    }
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Mail relay outbox publisher iteration failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
