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
    ILogger<RabbitMqMailRelayConsumerHostedService> logger,
    Func<CancellationToken, Task<IConnection>>? createConnectionAsync = null) : BackgroundService {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;
    private readonly Func<CancellationToken, Task<IConnection>> _createConnectionAsync =
        createConnectionAsync ?? (cancellationToken => CreateConnectionFactory(brokerOptions.Value).CreateConnectionAsync(cancellationToken));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal)) {
            logger.LogInformation("RabbitMQ relay consumer is disabled because backend mode is {Backend}.", _brokerOptions.Backend);
            return;
        }

        while (!stoppingToken.IsCancellationRequested) {
            try {
                IConnection connection = await _createConnectionAsync(stoppingToken);
                await using (connection) {
                    IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                    await using (channel) {
                        await RunConsumerAsync(channel, stoppingToken);
                    }
                }
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                logger.LogWarning(
                    exception,
                    "RabbitMQ relay consumer connection failed. Retrying in {RetryDelaySeconds} seconds.",
                    _brokerOptions.ConnectionRetryDelaySeconds);

                if (!await DelayBeforeRetryAsync(stoppingToken)) {
                    return;
                }
            }
        }
    }

    private async Task<bool> DelayBeforeRetryAsync(CancellationToken cancellationToken) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(_brokerOptions.ConnectionRetryDelaySeconds), cancellationToken);
            return true;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return false;
        }
    }

    private static ConnectionFactory CreateConnectionFactory(MailRelayBrokerOptions brokerOptions) {
        return new ConnectionFactory {
            HostName = brokerOptions.HostName,
            Port = brokerOptions.Port,
            UserName = brokerOptions.UserName,
            Password = brokerOptions.Password,
            VirtualHost = brokerOptions.VirtualHost,
        };
    }

    private async Task RunConsumerAsync(IChannel channel, CancellationToken stoppingToken) {
        await broker.DeclareTopologyAsync(stoppingToken);
        await channel.BasicQosAsync(0, _brokerOptions.PrefetchCount, global: false, cancellationToken: stoppingToken);

        AsyncEventingBasicConsumer consumer = CreateConsumer(channel, stoppingToken);
        string consumerTag = await channel.BasicConsumeAsync(
            queue: _brokerOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("RabbitMQ relay consumer started. Queue={QueueName}, ConsumerTag={ConsumerTag}", _brokerOptions.QueueName, consumerTag);

        // Wait until shutdown is requested. Completing a TaskCompletionSource from the cancellation
        // callback lets the wait finish normally, so there is no unreachable "delay elapsed" branch
        // (Task.Delay(Infinite) can only ever end by throwing) and no exception flow to handle.
        var stopRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration =
            stoppingToken.Register(static state => ((TaskCompletionSource)state!).TrySetResult(), stopRequested);
        await using (registration) {
            await stopRequested.Task;
        }

        logger.LogDebug("RabbitMQ relay consumer stop was requested.");

        if (channel.IsOpen) {
            await channel.BasicCancelAsync(consumerTag, noWait: false, cancellationToken: CancellationToken.None);
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
            await ProcessDeliveryAsync(channel, eventArgs, stoppingToken);
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
            if (channel.IsOpen) {
                await channel.BasicNackAsync(deliveryTag, multiple: false, requeue: true, cancellationToken: CancellationToken.None);
            }
        } catch (Exception ex) {
            await HandleDeliveryFailureAsync(channel, eventArgs, ex, stoppingToken);
        }
    }

    private async Task ProcessDeliveryAsync(IChannel channel, BasicDeliverEventArgs eventArgs, CancellationToken cancellationToken) {
        ulong deliveryTag = eventArgs.DeliveryTag;
        string bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        if (!Guid.TryParse(bodyText, out Guid rawQueuedEmailId)) {
            logger.LogWarning("RabbitMQ relay message payload is not a valid email id: {Payload}", bodyText);
            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);
            return;
        }

        var queuedEmailId = new QueuedEmailId(rawQueuedEmailId);
        QueuedEmailMessage? claimedMessage = await queueStore.TryClaimMessageByIdAsync(queuedEmailId, cancellationToken);
        if (claimedMessage is null) {
            logger.LogDebug("Queued email {QueuedEmailId} was not claimable when RabbitMQ delivered it.", queuedEmailId);
            await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);
            return;
        }

        await ProcessClaimedMessageAsync(queuedEmailId, claimedMessage, cancellationToken);
        await channel.BasicAckAsync(deliveryTag, multiple: false, cancellationToken: cancellationToken);
    }

    private async Task ProcessClaimedMessageAsync(QueuedEmailId queuedEmailId, QueuedEmailMessage claimedMessage, CancellationToken cancellationToken) {
        MailRelayProcessResult result = await messageProcessor.ProcessAsync(claimedMessage, cancellationToken);
        await (result switch {
            MailRelayProcessResult.TerminalFailure => broker.PublishDeadLetterAsync(queuedEmailId, cancellationToken),
            MailRelayProcessResult.Sent or MailRelayProcessResult.Retry or MailRelayProcessResult.Recoverable => Task.CompletedTask,
            null => throw new InvalidOperationException("Mail relay processing must return an outcome."),
        });
    }

    private async Task HandleDeliveryFailureAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        Exception exception,
        CancellationToken cancellationToken) {
        logger.LogError(
            "RabbitMQ relay consumer failed to process delivery {DeliveryTag}. ErrorType={ErrorType}. " +
            "The PostgreSQL polling fallback remains the durable recovery path.",
            eventArgs.DeliveryTag,
            exception.GetType().Name);
        if (channel.IsOpen) {
            await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
    }
}
