using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Globalization;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayBroker(
    IOptions<MailRelayBrokerOptions> brokerOptions,
    ILogger<RabbitMqMailRelayBroker> logger) {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;

    public bool IsEnabled =>
        string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal);

    public async Task DeclareTopologyAsync(CancellationToken cancellationToken) {
        if (!IsEnabled) {
            return;
        }

        ConnectionFactory factory = CreateFactory();
        IConnection connection = await factory.CreateConnectionAsync(cancellationToken);
        await using (connection) {
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await using (channel) {
                await DeclareExchangesAsync(channel, cancellationToken);
                await DeclareOutboundQueueAsync(channel, cancellationToken);
                await DeclareRetryQueueAsync(channel, cancellationToken);
                await DeclareDeadLetterQueueAsync(channel, cancellationToken);

                logger.LogInformation(
                    "RabbitMQ MailRelay topology is ready. MainQueue={MainQueue}, RetryQueue={RetryQueue}, DeadLetterQueue={DeadLetterQueue}",
                    _brokerOptions.QueueName,
                    _brokerOptions.RetryQueueName,
                    _brokerOptions.DeadLetterQueueName);
            }
        }
    }

    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        if (!IsEnabled) {
            return;
        }

        ConnectionFactory factory = CreateFactory();
        IConnection connection = await factory.CreateConnectionAsync(cancellationToken);
        await using (connection) {
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await using (channel) {
                await channel.QueueDeclarePassiveAsync(_brokerOptions.QueueName, cancellationToken);
            }
        }
    }

    private async Task DeclareExchangesAsync(IChannel channel, CancellationToken cancellationToken) {
        await channel.ExchangeDeclareAsync(_brokerOptions.OutboundExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_brokerOptions.RetryExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_brokerOptions.DeadLetterExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
    }

    private async Task DeclareOutboundQueueAsync(IChannel channel, CancellationToken cancellationToken) {
        await channel.QueueDeclareAsync(
            queue: _brokerOptions.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: _brokerOptions.QueueName,
            exchange: _brokerOptions.OutboundExchangeName,
            routingKey: _brokerOptions.OutboundRoutingKey,
            cancellationToken: cancellationToken);
    }

    private async Task DeclareRetryQueueAsync(IChannel channel, CancellationToken cancellationToken) {
        var retryArguments = new Dictionary<string, object?>(StringComparer.Ordinal) {
            ["x-message-ttl"] = _brokerOptions.RetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = _brokerOptions.OutboundExchangeName,
            ["x-dead-letter-routing-key"] = _brokerOptions.OutboundRoutingKey,
        };

        await channel.QueueDeclareAsync(
            queue: _brokerOptions.RetryQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: retryArguments,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: _brokerOptions.RetryQueueName,
            exchange: _brokerOptions.RetryExchangeName,
            routingKey: _brokerOptions.RetryRoutingKey,
            cancellationToken: cancellationToken);
    }

    private async Task DeclareDeadLetterQueueAsync(IChannel channel, CancellationToken cancellationToken) {
        await channel.QueueDeclareAsync(
            queue: _brokerOptions.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: _brokerOptions.DeadLetterQueueName,
            exchange: _brokerOptions.DeadLetterExchangeName,
            routingKey: _brokerOptions.DeadLetterRoutingKey,
            cancellationToken: cancellationToken);
    }

    public Task PublishOutboundAsync(QueuedEmailId emailId, CancellationToken cancellationToken) {
        return PublishAsync(_brokerOptions.OutboundExchangeName, _brokerOptions.OutboundRoutingKey, emailId, cancellationToken);
    }

    public Task PublishRetryAsync(QueuedEmailId emailId, TimeSpan delay, CancellationToken cancellationToken) {
        TimeSpan boundedDelay = delay > TimeSpan.Zero
            ? delay
            : TimeSpan.FromMilliseconds(_brokerOptions.RetryDelayMilliseconds);
        return PublishAsync(_brokerOptions.RetryExchangeName, _brokerOptions.RetryRoutingKey, emailId, boundedDelay, cancellationToken);
    }

    public Task PublishDeadLetterAsync(QueuedEmailId emailId, CancellationToken cancellationToken) {
        return PublishAsync(_brokerOptions.DeadLetterExchangeName, _brokerOptions.DeadLetterRoutingKey, emailId, delay: null, cancellationToken);
    }

    private Task PublishAsync(string exchangeName, string routingKey, QueuedEmailId emailId, CancellationToken cancellationToken) =>
        PublishAsync(exchangeName, routingKey, emailId, delay: null, cancellationToken);

    private async Task PublishAsync(
        string exchangeName,
        string routingKey,
        QueuedEmailId emailId,
        TimeSpan? delay,
        CancellationToken cancellationToken) {
        if (!IsEnabled) {
            return;
        }

        ConnectionFactory factory = CreateFactory();
        IConnection connection = await factory.CreateConnectionAsync(cancellationToken);
        await using (connection) {
            IChannel channel = await connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: true,
                    outstandingPublisherConfirmationsRateLimiter: null,
                    consumerDispatchConcurrency: null),
                cancellationToken);
            await using (channel) {

                byte[] body = Encoding.UTF8.GetBytes(emailId.Value.ToString("D"));
                var properties = new BasicProperties {
                    Persistent = true,
                    ContentType = "text/plain",
                    MessageId = emailId.Value.ToString("D"),
                };
                if (delay is { } retryDelay) {
                    properties.Expiration = Math.Max(1, (long)retryDelay.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
                }

                await channel.BasicPublishAsync(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private ConnectionFactory CreateFactory() {
        return new ConnectionFactory {
            HostName = _brokerOptions.HostName,
            Port = _brokerOptions.Port,
            UserName = _brokerOptions.UserName,
            Password = _brokerOptions.Password,
            VirtualHost = _brokerOptions.VirtualHost,
        };
    }
}
