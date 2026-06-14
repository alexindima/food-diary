using System.Text;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using FoodDiary.MailRelay.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace FoodDiary.MailRelay.Tests;

[Collection("mailrelay-environment")]
[ExcludeFromCodeCoverage]
public sealed class RabbitMqMailRelayConsumerIntegrationTests(MailRelayEnvironmentFixture fixture) {
    [RequiresDockerFact]
    public async Task Consumer_AcksInvalidAndUnclaimableMessages() {
        fixture.EnsureAvailable();
        MailRelayBrokerOptions options = CreateOptions();
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBroker>.Instance);
        var queueStore = new RecordingQueueStore();
        var processor = new MailRelayMessageProcessor(
            queueStore,
            new SmtpSubmissionService(new RecordingRelayDeliveryTransport()),
            NullLogger<MailRelayMessageProcessor>.Instance);
        var consumer = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(options),
            broker,
            queueStore,
            processor,
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);

        await broker.DeclareTopologyAsync(CancellationToken.None);
        await consumer.StartAsync(CancellationToken.None);
        try {
            await PublishRawMessageAsync(options, "not-a-guid");
            await WaitUntilQueueIsEmptyAsync(options);

            var id = Guid.NewGuid();
            await PublishRawMessageAsync(options, id.ToString("D"));
            await WaitUntilQueueIsEmptyAsync(options);

            Assert.Equal(id, Assert.Single(queueStore.ClaimAttempts));
        } finally {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [RequiresDockerFact]
    public async Task Consumer_WhenProcessingIsTerminalFailure_PublishesDeadLetterAndAcksMessage() {
        fixture.EnsureAvailable();
        MailRelayBrokerOptions options = CreateOptions();
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBroker>.Instance);
        var id = Guid.NewGuid();
        var queueStore = new RecordingQueueStore {
            ClaimedMessage = CreateQueuedMessage(id),
            SuppressedRecipients = ["recipient@example.com"],
        };
        var processor = new MailRelayMessageProcessor(
            queueStore,
            new SmtpSubmissionService(new RecordingRelayDeliveryTransport()),
            NullLogger<MailRelayMessageProcessor>.Instance);
        var consumer = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(options),
            broker,
            queueStore,
            processor,
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);

        await broker.DeclareTopologyAsync(CancellationToken.None);
        await consumer.StartAsync(CancellationToken.None);
        try {
            await PublishRawMessageAsync(options, id.ToString("D"));
            await WaitUntilQueueIsEmptyAsync(options);

            Assert.Equal(1u, await GetQueueMessageCountAsync(options, options.DeadLetterQueueName));
        } finally {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    [RequiresDockerFact]
    public async Task Consumer_WhenClaimThrows_AcksMessageAsFailedDelivery() {
        fixture.EnsureAvailable();
        MailRelayBrokerOptions options = CreateOptions();
        var broker = new RabbitMqMailRelayBroker(
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBroker>.Instance);
        var queueStore = new RecordingQueueStore {
            ClaimException = new InvalidOperationException("claim failed"),
        };
        var processor = new MailRelayMessageProcessor(
            queueStore,
            new SmtpSubmissionService(new RecordingRelayDeliveryTransport()),
            NullLogger<MailRelayMessageProcessor>.Instance);
        var consumer = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(options),
            broker,
            queueStore,
            processor,
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);

        await broker.DeclareTopologyAsync(CancellationToken.None);
        await consumer.StartAsync(CancellationToken.None);
        try {
            await PublishRawMessageAsync(options, Guid.NewGuid().ToString("D"));
            await WaitUntilQueueIsEmptyAsync(options);

            Assert.True(queueStore.ClaimFailed);
        } finally {
            await consumer.StopAsync(CancellationToken.None);
        }
    }

    private MailRelayBrokerOptions CreateOptions() {
        string suffix = Guid.NewGuid().ToString("N");
        return new MailRelayBrokerOptions {
            Backend = MailRelayBrokerOptions.RabbitMqBackend,
            HostName = fixture.RabbitMqHostName,
            Port = fixture.RabbitMqPort,
            UserName = "guest",
            Password = "guest",
            QueueName = $"fooddiary.mailrelay.consumer.outbound.{suffix}",
            OutboundExchangeName = $"fooddiary.mailrelay.consumer.{suffix}",
            RetryExchangeName = $"fooddiary.mailrelay.consumer.retry.{suffix}",
            RetryQueueName = $"fooddiary.mailrelay.consumer.outbound.retry.{suffix}",
            DeadLetterExchangeName = $"fooddiary.mailrelay.consumer.dead.{suffix}",
            DeadLetterQueueName = $"fooddiary.mailrelay.consumer.outbound.dead.{suffix}",
            RetryDelayMilliseconds = 50,
        };
    }

    private static async Task PublishRawMessageAsync(MailRelayBrokerOptions options, string message) {
        ConnectionFactory factory = CreateConnectionFactory(options);
        IConnection connection = await factory.CreateConnectionAsync(CancellationToken.None).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await using (channel.ConfigureAwait(false)) {
                await channel.BasicPublishAsync(
                    exchange: options.OutboundExchangeName,
                    routingKey: options.OutboundRoutingKey,
                    mandatory: true,
                    body: Encoding.UTF8.GetBytes(message),
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private static async Task WaitUntilQueueIsEmptyAsync(MailRelayBrokerOptions options) {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline) {
            if (await GetQueueMessageCountAsync(options, options.QueueName).ConfigureAwait(false) == 0) {
                return;
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        throw new TimeoutException($"RabbitMQ queue '{options.QueueName}' was not drained.");
    }

    private static async Task<uint> GetQueueMessageCountAsync(MailRelayBrokerOptions options, string queueName) {
        ConnectionFactory factory = CreateConnectionFactory(options);
        IConnection connection = await factory.CreateConnectionAsync(CancellationToken.None).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            IChannel channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken.None).ConfigureAwait(false);
            await using (channel.ConfigureAwait(false)) {
                QueueDeclareOk result = await channel.QueueDeclarePassiveAsync(
                    queueName,
                    CancellationToken.None).ConfigureAwait(false);
                return result.MessageCount;
            }
        }
    }

    private static ConnectionFactory CreateConnectionFactory(MailRelayBrokerOptions options) =>
        new() {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
        };

    private sealed class RecordingRelayDeliveryTransport : IRelayDeliveryTransport {
        public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        public List<Guid> ClaimAttempts { get; } = [];
        public QueuedEmailMessage? ClaimedMessage { get; init; }
        public IReadOnlyList<string> SuppressedRecipients { get; init; } = [];
        public Exception? ClaimException { get; init; }
        public bool ClaimFailed { get; private set; }

        public Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task<IReadOnlyList<QueuedEmailMessage>> ClaimDueBatchAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QueuedEmailMessage>>([]);

        public Task<QueuedEmailMessage?> TryClaimMessageByIdAsync(Guid id, CancellationToken cancellationToken) {
            ClaimAttempts.Add(id);
            if (ClaimException is not null) {
                ClaimFailed = true;
                return Task.FromException<QueuedEmailMessage?>(ClaimException);
            }

            return Task.FromResult(ClaimedMessage);
        }

        public Task<IReadOnlyList<MailRelayOutboxMessage>> ClaimOutboxBatchAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MailRelayOutboxMessage>>([]);

        public Task MarkOutboxPublishedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkOutboxFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MailRelayInboxClaimResult> TryClaimInboxMessageAsync(
            string consumerName,
            string messageKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MailRelayInboxClaimResult(Claimed: true, Guid.NewGuid()));

        public Task MarkInboxProcessedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkInboxFailedAsync(Guid id, string error, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkSentAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkSuppressedAsync(Guid id, IReadOnlyCollection<string> recipients, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(string? email, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MailRelaySuppressionEntry>>([]);

        public Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(IngestMailEventRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new MailRelayDeliveryEventEntry(
                Guid.NewGuid(),
                request.EventType,
                request.Email,
                request.Source,
                request.Classification,
                request.ProviderMessageId,
                request.Reason,
                request.OccurredAtUtc ?? DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));

        public Task<IReadOnlyList<MailRelayDeliveryEventEntry>> GetDeliveryEventsAsync(string? email, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MailRelayDeliveryEventEntry>>([]);

        public Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(IReadOnlyCollection<string> recipients, CancellationToken cancellationToken) =>
            Task.FromResult(SuppressedRecipients);

        public Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new MailRelayQueueStats(0, 0, 0, 0, 0, 0));

        public Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<MailRelayMessageDetails?>(null);

        public Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(null);
    }

    private static QueuedEmailMessage CreateQueuedMessage(Guid id) =>
        new(
            id,
            "relay@example.com",
            "FoodDiary",
            ["recipient@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello",
            "correlation",
            AttemptCount: 1,
            MaxAttempts: 3);
}
