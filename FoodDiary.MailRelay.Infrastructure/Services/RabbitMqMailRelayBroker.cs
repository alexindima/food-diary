using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class RabbitMqMailRelayBroker(
    IOptions<MailRelayBrokerOptions> brokerOptions,
    ILogger<RabbitMqMailRelayBroker> logger) {
    private readonly MailRelayBrokerOptions _brokerOptions = brokerOptions.Value;
    private readonly ILogger<RabbitMqMailRelayBroker> _logger = logger;

    public bool IsEnabled =>
        string.Equals(_brokerOptions.Backend, MailRelayBrokerOptions.RabbitMqBackend, StringComparison.Ordinal);

    public async Task DeclareTopologyAsync(CancellationToken cancellationToken) {
        if (!IsEnabled) {
            return;
        }

        var factory = CreateFactory();
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(_brokerOptions.OutboundExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_brokerOptions.RetryExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_brokerOptions.DeadLetterExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);

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

        var retryArguments = new Dictionary<string, object?> {
            ["x-message-ttl"] = _brokerOptions.RetryDelayMilliseconds,
            ["x-dead-letter-exchange"] = _brokerOptions.OutboundExchangeName,
            ["x-dead-letter-routing-key"] = _brokerOptions.OutboundRoutingKey
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

        _logger.LogInformation(
            "RabbitMQ MailRelay topology is ready. MainQueue={MainQueue}, RetryQueue={RetryQueue}, DeadLetterQueue={DeadLetterQueue}",
            _brokerOptions.QueueName,
            _brokerOptions.RetryQueueName,
            _brokerOptions.DeadLetterQueueName);
    }

    public Task PublishOutboundAsync(Guid emailId, CancellationToken cancellationToken) {
        return PublishAsync(_brokerOptions.OutboundExchangeName, _brokerOptions.OutboundRoutingKey, emailId, cancellationToken);
    }

    public Task PublishRetryAsync(Guid emailId, CancellationToken cancellationToken) {
        return PublishAsync(_brokerOptions.RetryExchangeName, _brokerOptions.RetryRoutingKey, emailId, cancellationToken);
    }

    public Task PublishDeadLetterAsync(Guid emailId, CancellationToken cancellationToken) {
        return PublishAsync(_brokerOptions.DeadLetterExchangeName, _brokerOptions.DeadLetterRoutingKey, emailId, cancellationToken);
    }

    private async Task PublishAsync(string exchangeName, string routingKey, Guid emailId, CancellationToken cancellationToken) {
        if (!IsEnabled) {
            return;
        }

        var factory = CreateFactory();
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true,
                outstandingPublisherConfirmationsRateLimiter: null,
                consumerDispatchConcurrency: null),
            cancellationToken);

        var body = Encoding.UTF8.GetBytes(emailId.ToString("D"));
        var properties = new BasicProperties {
            Persistent = true,
            ContentType = "text/plain",
            MessageId = emailId.ToString("D")
        };

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }

    private ConnectionFactory CreateFactory() {
        return new ConnectionFactory {
            HostName = _brokerOptions.HostName,
            Port = _brokerOptions.Port,
            UserName = _brokerOptions.UserName,
            Password = _brokerOptions.Password,
            VirtualHost = _brokerOptions.VirtualHost
        };
    }
}
