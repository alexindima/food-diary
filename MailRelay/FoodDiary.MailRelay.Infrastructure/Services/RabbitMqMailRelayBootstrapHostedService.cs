using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayBootstrapHostedService(
    RabbitMqMailRelayBroker broker,
    IOptions<MailRelayBrokerOptions> brokerOptions,
    ILogger<RabbitMqMailRelayBootstrapHostedService> logger) : IHostedService {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;

    public async Task StartAsync(CancellationToken cancellationToken) {
        if (!broker.IsEnabled) {
            return;
        }

        while (!cancellationToken.IsCancellationRequested) {
            try {
                await broker.DeclareTopologyAsync(cancellationToken).ConfigureAwait(false);
                logger.LogInformation("RabbitMQ topology bootstrap completed for MailRelay.");
                return;
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                logger.LogWarning(
                    exception,
                    "RabbitMQ topology bootstrap failed. Retrying in {RetryDelaySeconds} seconds.",
                    _brokerOptions.ConnectionRetryDelaySeconds);

                if (!await DelayBeforeRetryAsync(cancellationToken).ConfigureAwait(false)) {
                    return;
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
