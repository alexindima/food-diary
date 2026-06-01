using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayConsumerHostedService(
    IOptions<MailRelayBrokerOptions> brokerOptions,
    RabbitMqMailRelayBroker broker,
    IMailRelayQueueStore queueStore,
    MailRelayMessageProcessor messageProcessor,
    ILogger<RabbitMqMailRelayConsumerHostedService> logger) : BackgroundService {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;
    private const string ConsumerName = "mailrelay-rabbitmq-consumer";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal)) {
            logger.LogInformation("RabbitMQ relay consumer is disabled because backend mode is {Backend}.", _brokerOptions.Backend);
            return;
        }

        var factory = new ConnectionFactory {
            HostName = _brokerOptions.HostName,
            Port = _brokerOptions.Port,
            UserName = _brokerOptions.UserName,
            Password = _brokerOptions.Password,
            VirtualHost = _brokerOptions.VirtualHost
        };

        var connection = await factory.CreateConnectionAsync(stoppingToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
            await using (channel.ConfigureAwait(false)) {
                await broker.DeclareTopologyAsync(stoppingToken).ConfigureAwait(false);
                await channel.BasicQosAsync(0, _brokerOptions.PrefetchCount, global: false, cancellationToken: stoppingToken).ConfigureAwait(false);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, eventArgs) => {
                    var deliveryTag = eventArgs.DeliveryTag;
                    try {
                        var bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                        if (!Guid.TryParse(bodyText, out var queuedEmailId)) {
                            logger.LogWarning("RabbitMQ relay message payload is not a valid email id: {Payload}", bodyText);
                            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken).ConfigureAwait(false);
                            return;
                        }

                        var inboxClaim = await queueStore.TryClaimInboxMessageAsync(ConsumerName, queuedEmailId.ToString("D"), stoppingToken).ConfigureAwait(false);
                        if (!inboxClaim.Claimed) {
                            logger.LogDebug("Queued email {QueuedEmailId} was already processed via inbox dedup.", queuedEmailId);
                            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken).ConfigureAwait(false);
                            return;
                        }

                        var claimedMessage = await queueStore.TryClaimMessageByIdAsync(queuedEmailId, stoppingToken).ConfigureAwait(false);
                        if (claimedMessage is null) {
                            logger.LogDebug("Queued email {QueuedEmailId} was not claimable when RabbitMQ delivered it.", queuedEmailId);
                            await queueStore.MarkInboxProcessedAsync(inboxClaim.InboxId, stoppingToken).ConfigureAwait(false);
                            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken).ConfigureAwait(false);
                            return;
                        }

                        var result = await messageProcessor.ProcessAsync(claimedMessage, stoppingToken).ConfigureAwait(false);
                        if (!result.Succeeded) {
                            if (result.IsTerminalFailure) {
                                await broker.PublishDeadLetterAsync(queuedEmailId, stoppingToken).ConfigureAwait(false);
                            } else {
                                await broker.PublishRetryAsync(queuedEmailId, stoppingToken).ConfigureAwait(false);
                            }
                        }

                        await queueStore.MarkInboxProcessedAsync(inboxClaim.InboxId, stoppingToken).ConfigureAwait(false);
                        await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken).ConfigureAwait(false);
                    } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                        if (channel.IsOpen) {
                            await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                        }
                    } catch (Exception ex) {
                        logger.LogError(ex, "RabbitMQ relay consumer failed to process a delivery.");
                        if (Guid.TryParse(Encoding.UTF8.GetString(eventArgs.Body.ToArray()), out var queuedEmailId)) {
                            var inboxClaim = await queueStore.TryClaimInboxMessageAsync(ConsumerName, queuedEmailId.ToString("D"), CancellationToken.None).ConfigureAwait(false);
                            await queueStore.MarkInboxFailedAsync(inboxClaim.InboxId, ex.ToString(), CancellationToken.None).ConfigureAwait(false);
                        }

                        if (channel.IsOpen) {
                            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken).ConfigureAwait(false);
                        }
                    }
                };

                var consumerTag = await channel.BasicConsumeAsync(
                    queue: _brokerOptions.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken).ConfigureAwait(false);

                logger.LogInformation("RabbitMQ relay consumer started. Queue={QueueName}, ConsumerTag={ConsumerTag}", _brokerOptions.QueueName, consumerTag);

                try {
                    await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                }

                if (channel.IsOpen) {
                    await channel.BasicCancelAsync(consumerTag, noWait: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
