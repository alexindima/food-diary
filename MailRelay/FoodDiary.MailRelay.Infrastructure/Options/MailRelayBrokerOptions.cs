namespace FoodDiary.MailRelay.Infrastructure.Options;

public sealed class MailRelayBrokerOptions {
    public const string SectionName = "MailRelayBroker";
    public const string PostgresPollingBackend = "PostgresPolling";
    public const string RabbitMqBackend = "RabbitMq";

    public string Backend { get; init; } = RabbitMqBackend;
    public string HostName { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string QueueName { get; init; } = "fooddiary.mailrelay.outbound";
    public ushort PrefetchCount { get; init; } = 10;
    public bool EnablePollingFallback { get; init; } = true;
    public string OutboundExchangeName { get; init; } = "fooddiary.mailrelay";
    public string OutboundRoutingKey { get; init; } = "outbound";
    public string RetryExchangeName { get; init; } = "fooddiary.mailrelay.retry";
    public string RetryQueueName { get; init; } = "fooddiary.mailrelay.outbound.retry";
    public string RetryRoutingKey { get; init; } = "retry";
    public int RetryDelayMilliseconds { get; init; } = 30000;
    public string DeadLetterExchangeName { get; init; } = "fooddiary.mailrelay.dead";
    public string DeadLetterQueueName { get; init; } = "fooddiary.mailrelay.outbound.dead";
    public string DeadLetterRoutingKey { get; init; } = "dead";

    public static bool HasSupportedBackend(MailRelayBrokerOptions options) {
        return options.Backend is PostgresPollingBackend or RabbitMqBackend;
    }

    public static bool HasValidConfiguration(MailRelayBrokerOptions options) {
        if (options.Port <= 0 || options.PrefetchCount == 0 || options.RetryDelayMilliseconds <= 0) {
            return false;
        }

        if (string.Equals(options.Backend, PostgresPollingBackend, StringComparison.Ordinal)) {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.HostName) &&
               !string.IsNullOrWhiteSpace(options.UserName) &&
               !string.IsNullOrWhiteSpace(options.QueueName) &&
               !string.IsNullOrWhiteSpace(options.OutboundExchangeName) &&
               !string.IsNullOrWhiteSpace(options.OutboundRoutingKey) &&
               !string.IsNullOrWhiteSpace(options.RetryExchangeName) &&
               !string.IsNullOrWhiteSpace(options.RetryQueueName) &&
               !string.IsNullOrWhiteSpace(options.RetryRoutingKey) &&
               !string.IsNullOrWhiteSpace(options.DeadLetterExchangeName) &&
               !string.IsNullOrWhiteSpace(options.DeadLetterQueueName) &&
               !string.IsNullOrWhiteSpace(options.DeadLetterRoutingKey);
    }
}
