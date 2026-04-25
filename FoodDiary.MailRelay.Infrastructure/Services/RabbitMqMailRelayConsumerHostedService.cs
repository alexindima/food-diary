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

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await broker.DeclareTopologyAsync(stoppingToken);
        await channel.BasicQosAsync(0, _brokerOptions.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) => {
            var deliveryTag = eventArgs.DeliveryTag;
            try {
                var bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                if (!Guid.TryParse(bodyText, out var queuedEmailId)) {
                    logger.LogWarning("RabbitMQ relay message payload is not a valid email id: {Payload}", bodyText);
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                var inboxClaim = await queueStore.TryClaimInboxMessageAsync(ConsumerName, queuedEmailId.ToString("D"), stoppingToken);
                if (!inboxClaim.Claimed) {
                    logger.LogDebug("Queued email {QueuedEmailId} was already processed via inbox dedup.", queuedEmailId);
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                var claimedMessage = await queueStore.TryClaimMessageByIdAsync(queuedEmailId, stoppingToken);
                if (claimedMessage is null) {
                    logger.LogDebug("Queued email {QueuedEmailId} was not claimable when RabbitMQ delivered it.", queuedEmailId);
                    await queueStore.MarkInboxProcessedAsync(inboxClaim.InboxId, stoppingToken);
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                var result = await messageProcessor.ProcessAsync(claimedMessage, stoppingToken);
                if (!result.Succeeded) {
                    if (result.IsTerminalFailure) {
                        await broker.PublishDeadLetterAsync(queuedEmailId, stoppingToken);
                    } else {
                        await broker.PublishRetryAsync(queuedEmailId, stoppingToken);
                    }
                }

                await queueStore.MarkInboxProcessedAsync(inboxClaim.InboxId, stoppingToken);
                await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                if (channel.IsOpen) {
                    await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
                }
            } catch (Exception ex) {
                logger.LogError(ex, "RabbitMQ relay consumer failed to process a delivery.");
                if (Guid.TryParse(Encoding.UTF8.GetString(eventArgs.Body.ToArray()), out var queuedEmailId)) {
                    var inboxClaim = await queueStore.TryClaimInboxMessageAsync(ConsumerName, queuedEmailId.ToString("D"), CancellationToken.None);
                    await queueStore.MarkInboxFailedAsync(inboxClaim.InboxId, ex.ToString(), CancellationToken.None);
                }

                if (channel.IsOpen) {
                    await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
            }
        };

        var consumerTag = await channel.BasicConsumeAsync(
            queue: _brokerOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("RabbitMQ relay consumer started. Queue={QueueName}, ConsumerTag={ConsumerTag}", _brokerOptions.QueueName, consumerTag);

        try {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
        }

        if (channel.IsOpen) {
            await channel.BasicCancelAsync(consumerTag, noWait: false, cancellationToken: CancellationToken.None);
        }
    }
}
