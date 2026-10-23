using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayBootstrapHostedService(
    RabbitMqMailRelayBroker broker,
    IOptions<MailRelayBrokerOptions> brokerOptions,
    ILogger<RabbitMqMailRelayBootstrapHostedService> logger,
    Func<CancellationToken, Task>? declareTopologyAsync = null) : BackgroundService {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;
    private readonly Func<CancellationToken, Task> _declareTopologyAsync =
        declareTopologyAsync ?? broker.DeclareTopologyAsync;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!broker.IsEnabled) {
            return;
        }

        while (!stoppingToken.IsCancellationRequested) {
            try {
                await _declareTopologyAsync(stoppingToken).ConfigureAwait(false);
                logger.LogInformation("RabbitMQ topology bootstrap completed for MailRelay.");
                return;
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                logger.LogWarning(
                    exception,
                    "RabbitMQ topology bootstrap failed. Retrying in {RetryDelaySeconds} seconds.",
                    _brokerOptions.ConnectionRetryDelaySeconds);

                if (!await DelayBeforeRetryAsync(stoppingToken).ConfigureAwait(false)) {
                    return;
                }
            }
        }
    }

    private async Task<bool> DelayBeforeRetryAsync(CancellationToken cancellationToken) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(_brokerOptions.ConnectionRetryDelaySeconds), cancellationToken)
                .ConfigureAwait(false);
            return true;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return false;
        }
    }
}
