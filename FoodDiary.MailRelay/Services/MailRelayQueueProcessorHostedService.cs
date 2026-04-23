using FoodDiary.MailRelay.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Services;

public sealed class MailRelayQueueProcessorHostedService(
    MailRelayQueueStore queueStore,
    MailRelayMessageProcessor messageProcessor,
    IOptions<MailRelayBrokerOptions> brokerOptions,
    IOptions<MailRelayQueueOptions> queueOptions,
    ILogger<MailRelayQueueProcessorHostedService> logger) : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (string.Equals(brokerOptions.Value.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal) &&
            !brokerOptions.Value.EnablePollingFallback) {
            logger.LogInformation("Mail relay polling worker is disabled because RabbitMQ backend is active and polling fallback is off.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(queueOptions.Value.PollIntervalSeconds));

        do {
            try {
                var claimedMessages = await queueStore.ClaimDueBatchAsync(stoppingToken);
                foreach (var message in claimedMessages) {
                    await messageProcessor.ProcessAsync(message, stoppingToken);
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            } catch (Exception ex) {
                logger.LogError(ex, "Mail relay queue processor iteration failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
