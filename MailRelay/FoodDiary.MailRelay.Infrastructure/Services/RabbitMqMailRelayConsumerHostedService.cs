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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal)) {
            logger.LogInformation("RabbitMQ relay consumer is disabled because backend mode is {Backend}.", _brokerOptions.Backend);
            return;
        }

        IConnection connection = await CreateConnectionFactory().CreateConnectionAsync(stoppingToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
            await using (channel.ConfigureAwait(false)) {
                await RunConsumerAsync(channel, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private ConnectionFactory CreateConnectionFactory() {
        return new ConnectionFactory {
            HostName = _brokerOptions.HostName,
            Port = _brokerOptions.Port,
            UserName = _brokerOptions.UserName,
            Password = _brokerOptions.Password,
            VirtualHost = _brokerOptions.VirtualHost,
        };
    }

    private async Task RunConsumerAsync(IChannel channel, CancellationToken stoppingToken) {
        await broker.DeclareTopologyAsync(stoppingToken).ConfigureAwait(false);
        await channel.BasicQosAsync(0, _brokerOptions.PrefetchCount, global: false, cancellationToken: stoppingToken).ConfigureAwait(false);

        AsyncEventingBasicConsumer consumer = CreateConsumer(channel, stoppingToken);
        string consumerTag = await channel.BasicConsumeAsync(
            queue: _brokerOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken).ConfigureAwait(false);

        logger.LogInformation("RabbitMQ relay consumer started. Queue={QueueName}, ConsumerTag={ConsumerTag}", _brokerOptions.QueueName, consumerTag);

        try {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            logger.LogDebug("RabbitMQ relay consumer stop was requested.");
        }

        if (channel.IsOpen) {
            await channel.BasicCancelAsync(consumerTag, noWait: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
    }

    private AsyncEventingBasicConsumer CreateConsumer(IChannel channel, CancellationToken stoppingToken) {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, eventArgs) => HandleDeliveryAsync(channel, eventArgs, stoppingToken);
        return consumer;
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken stoppingToken) {
        ulong deliveryTag = eventArgs.DeliveryTag;

        try {
            await ProcessDeliveryAsync(channel, eventArgs, stoppingToken).ConfigureAwait(false);
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            if (channel.IsOpen) {
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
        } catch (Exception ex) {
            await HandleDeliveryFailureAsync(channel, eventArgs, ex, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessDeliveryAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken) {
        ulong deliveryTag = eventArgs.DeliveryTag;
        string bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        if (!Guid.TryParse(bodyText, out Guid queuedEmailId)) {
            logger.LogWarning("RabbitMQ relay message payload is not a valid email id: {Payload}", bodyText);
            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        QueuedEmailMessage? claimedMessage = await queueStore.TryClaimMessageByIdAsync(queuedEmailId, cancellationToken).ConfigureAwait(false);
        if (claimedMessage is null) {
            logger.LogDebug("Queued email {QueuedEmailId} was not claimable when RabbitMQ delivered it.", queuedEmailId);
            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        await ProcessClaimedMessageAsync(queuedEmailId, claimedMessage, cancellationToken).ConfigureAwait(false);
        await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task ProcessClaimedMessageAsync(Guid queuedEmailId, QueuedEmailMessage claimedMessage, CancellationToken cancellationToken) {
        MailRelayProcessResult result = await messageProcessor.ProcessAsync(claimedMessage, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded) {
            return;
        }

        if (result.IsTerminalFailure) {
            await broker.PublishDeadLetterAsync(queuedEmailId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleDeliveryFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        Exception exception,
        CancellationToken cancellationToken) {
        logger.LogError(exception, "RabbitMQ relay consumer failed to process a delivery.");
        if (channel.IsOpen) {
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
