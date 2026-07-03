using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Infrastructure.Options;
using FoodDiary.MailRelay.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayHostedServiceTests {
    [Fact]
    public async Task OutboxPublisher_WhenBackendIsNotRabbitMq_ReturnsWithoutPollingStore() {
        var store = new RecordingQueueStore();
        var service = new MailRelayOutboxPublisherHostedService(
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            Options.Create(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            NullLogger<MailRelayOutboxPublisherHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);

        Assert.False(store.OutboxClaimed);
    }

    [Fact]
    public async Task OutboxPublisher_WhenClaimThrows_LogsAndContinuesUntilStopped() {
        var store = new RecordingQueueStore {
            ClaimOutboxException = new InvalidOperationException("claim failed"),
        };
        var service = new MailRelayOutboxPublisherHostedService(
            CreateBroker(CreateRabbitOptions()),
            Options.Create(CreateRabbitOptions()),
            store,
            NullLogger<MailRelayOutboxPublisherHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await store.WaitForOutboxClaimAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.True(store.OutboxClaimed);
    }

    [Fact]
    public async Task OutboxPublisher_WhenStoppingTokenIsCanceled_BreaksLoop() {
        var store = new RecordingQueueStore {
            ThrowCancellationOnOutboxClaim = true,
        };
        var service = new MailRelayOutboxPublisherHostedService(
            CreateBroker(CreateRabbitOptions()),
            Options.Create(CreateRabbitOptions()),
            store,
            NullLogger<MailRelayOutboxPublisherHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await InvokeExecuteAsync(service, cancellationTokenSource.Token);
    }

    [Fact]
    public async Task OutboxPublisher_WhenPublishFails_MarksOutboxMessageFailed() {
        var store = new RecordingQueueStore {
            OutboxBatch = [new MailRelayOutboxMessage(Guid.NewGuid(), Guid.NewGuid(), AttemptCount: 2)],
        };
        MailRelayBrokerOptions options = CreateRabbitOptions(port: 1);
        var service = new MailRelayOutboxPublisherHostedService(
            CreateBroker(options),
            Options.Create(options),
            store,
            NullLogger<MailRelayOutboxPublisherHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await store.WaitForOutboxFailureAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.Equal(2, store.FailedOutboxAttemptCount);
        Assert.Contains("Rabbit", store.FailedOutboxError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueueProcessor_WhenRabbitMqBackendHasNoPollingFallback_ReturnsWithoutPollingStore() {
        var store = new RecordingQueueStore();
        var service = new MailRelayQueueProcessorHostedService(
            store,
            CreateProcessor(store),
            Options.Create(new MailRelayBrokerOptions {
                Backend = MailRelayBrokerOptions.RabbitMqBackend,
                EnablePollingFallback = false,
            }),
            Options.Create(new MailRelayQueueOptions { PollIntervalSeconds = 1 }),
            NullLogger<MailRelayQueueProcessorHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);

        Assert.False(store.QueueClaimed);
    }

    [Fact]
    public async Task QueueProcessor_WhenClaimThrows_LogsAndContinuesUntilStopped() {
        var store = new RecordingQueueStore {
            ClaimQueueException = new InvalidOperationException("claim failed"),
        };
        var service = new MailRelayQueueProcessorHostedService(
            store,
            CreateProcessor(store),
            Options.Create(new MailRelayBrokerOptions {
                Backend = MailRelayBrokerOptions.PostgresPollingBackend,
            }),
            Options.Create(new MailRelayQueueOptions { PollIntervalSeconds = 1 }),
            NullLogger<MailRelayQueueProcessorHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await store.WaitForQueueClaimAsync();
        await service.StopAsync(CancellationToken.None);

        Assert.True(store.QueueClaimed);
    }

    [Fact]
    public async Task QueueProcessor_WhenStoppingTokenIsCanceled_BreaksLoop() {
        var store = new RecordingQueueStore {
            ThrowCancellationOnQueueClaim = true,
        };
        var service = new MailRelayQueueProcessorHostedService(
            store,
            CreateProcessor(store),
            Options.Create(new MailRelayBrokerOptions {
                Backend = MailRelayBrokerOptions.PostgresPollingBackend,
            }),
            Options.Create(new MailRelayQueueOptions { PollIntervalSeconds = 1 }),
            NullLogger<MailRelayQueueProcessorHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await InvokeExecuteAsync(service, cancellationTokenSource.Token);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenBackendIsNotRabbitMq_ReturnsWithoutConnecting() {
        var store = new RecordingQueueStore();
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);

        Assert.False(store.QueueClaimed);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenRabbitMqIsUnavailable_RetriesUntilStopped() {
        var store = new RecordingQueueStore();
        MailRelayBrokerOptions options = CreateRabbitOptions(port: 1, connectionRetryDelaySeconds: 1);
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(options),
            CreateBroker(options),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        await InvokeExecuteAsync(service, cancellationTokenSource.Token);

        Assert.False(store.QueueClaimed);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenConnectionFactoryThrows_LogsAndStopsWhenRetryIsCanceled() {
        var store = new RecordingQueueStore();
        MailRelayBrokerOptions options = CreateRabbitOptions(connectionRetryDelaySeconds: 5);
        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(options),
            CreateBroker(options),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance,
            async _ => {
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                throw new InvalidOperationException("connection failed");
            });

        await InvokeExecuteAsync(service, cancellationTokenSource.Token);

        Assert.False(store.QueueClaimed);
    }

    [Fact]
    public async Task RabbitMqBootstrap_WhenRabbitMqIsUnavailable_RetriesUntilStopped() {
        MailRelayBrokerOptions options = CreateRabbitOptions(port: 1, connectionRetryDelaySeconds: 1);
        var service = new RabbitMqMailRelayBootstrapHostedService(
            CreateBroker(options),
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        await service.StartAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task RabbitMqBootstrap_WhenCancellationAlreadyRequested_DoesNotDeclareTopology() {
        MailRelayBrokerOptions options = CreateRabbitOptions();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var service = new RabbitMqMailRelayBootstrapHostedService(
            CreateBroker(options),
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance,
            _ => throw new InvalidOperationException("should not run"));

        await service.StartAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task RabbitMqBootstrap_WhenDeclareTopologyThrows_LogsAndStopsWhenRetryIsCanceled() {
        MailRelayBrokerOptions options = CreateRabbitOptions(connectionRetryDelaySeconds: 5);
        using var cancellationTokenSource = new CancellationTokenSource();
        var service = new RabbitMqMailRelayBootstrapHostedService(
            CreateBroker(options),
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance,
            async _ => {
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                throw new InvalidOperationException("bootstrap failed");
            });

        await service.StartAsync(cancellationTokenSource.Token);
    }

    [Fact]
    public async Task RabbitMqBootstrap_WhenRetryDelayCompletes_RetriesUntilCancellation() {
        MailRelayBrokerOptions options = CreateRabbitOptions(connectionRetryDelaySeconds: 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        int attempts = 0;
        var service = new RabbitMqMailRelayBootstrapHostedService(
            CreateBroker(options),
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance,
            async _ => {
                attempts++;
                if (attempts > 1) {
                    await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                    throw new OperationCanceledException(cancellationTokenSource.Token);
                }

                throw new InvalidOperationException("bootstrap failed");
            });

        await service.StartAsync(cancellationTokenSource.Token);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenStoppingDuringDelivery_NacksMessageForRetry() {
        var store = new RecordingQueueStore {
            ThrowCancellationOnMessageClaim = true,
        };
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(CreateRabbitOptions()),
            CreateBroker(CreateRabbitOptions()),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        var channelProxy = RabbitMqChannelProxy.Create();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await InvokeHandleDeliveryAsync(service, channelProxy.Channel, CreateDelivery(Guid.NewGuid()), cancellationTokenSource.Token);

        Assert.True(channelProxy.Nacked);
        Assert.Equal(42UL, channelProxy.NackedDeliveryTag);
        Assert.True(channelProxy.NackRequeue);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenPayloadIsInvalid_AcksDelivery() {
        var store = new RecordingQueueStore();
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(CreateRabbitOptions()),
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        var channelProxy = RabbitMqChannelProxy.Create();

        await InvokeHandleDeliveryAsync(service, channelProxy.Channel, CreateDelivery("not-a-guid"), CancellationToken.None);

        Assert.True(channelProxy.Acked);
        Assert.Equal(42UL, channelProxy.AckedDeliveryTag);
        Assert.False(store.MessageClaimed);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenMessageCannotBeClaimed_AcksDelivery() {
        var store = new RecordingQueueStore();
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(CreateRabbitOptions()),
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        var channelProxy = RabbitMqChannelProxy.Create();
        var emailId = Guid.NewGuid();

        await InvokeHandleDeliveryAsync(service, channelProxy.Channel, CreateDelivery(emailId), CancellationToken.None);

        Assert.True(channelProxy.Acked);
        Assert.Equal(42UL, channelProxy.AckedDeliveryTag);
        Assert.True(store.MessageClaimed);
        Assert.Equal(emailId, store.ClaimedMessageId);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenClaimedMessageSucceeds_AcksDeliveryAndMarksSent() {
        var store = new RecordingQueueStore {
            ClaimedMessage = CreateQueuedMessage(),
        };
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(CreateRabbitOptions()),
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        var channelProxy = RabbitMqChannelProxy.Create();

        await InvokeHandleDeliveryAsync(service, channelProxy.Channel, CreateDelivery(store.ClaimedMessage.Id), CancellationToken.None);

        Assert.True(channelProxy.Acked);
        Assert.Equal(store.ClaimedMessage.Id, store.SentMessageId);
    }

    [Fact]
    public async Task RabbitMqConsumer_WhenClaimedMessageIsSuppressed_AcksDeliveryAndMarksSuppressed() {
        var store = new RecordingQueueStore {
            ClaimedMessage = CreateQueuedMessage(),
            SuppressedRecipients = ["recipient@example.com"],
        };
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(CreateRabbitOptions()),
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        var channelProxy = RabbitMqChannelProxy.Create();

        await InvokeHandleDeliveryAsync(service, channelProxy.Channel, CreateDelivery(store.ClaimedMessage.Id), CancellationToken.None);

        Assert.True(channelProxy.Acked);
        Assert.Equal(store.ClaimedMessage.Id, store.SuppressedMessageId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RabbitMqConsumer_DelayBeforeRetry_ReturnsExpectedResultForCancellation(bool cancelBeforeDelay) {
        MailRelayBrokerOptions options = CreateRabbitOptions(connectionRetryDelaySeconds: 0);
        var store = new RecordingQueueStore();
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(options),
            CreateBroker(options),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        if (cancelBeforeDelay) {
            await cancellationTokenSource.CancelAsync();
        }

        bool delayed = await InvokeDelayBeforeRetryAsync(service, cancellationTokenSource.Token);

        Assert.Equal(!cancelBeforeDelay, delayed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RabbitMqBootstrap_DelayBeforeRetry_ReturnsExpectedResultForCancellation(bool cancelBeforeDelay) {
        MailRelayBrokerOptions options = CreateRabbitOptions(connectionRetryDelaySeconds: 0);
        var service = new RabbitMqMailRelayBootstrapHostedService(
            CreateBroker(options),
            Options.Create(options),
            NullLogger<RabbitMqMailRelayBootstrapHostedService>.Instance);
        using var cancellationTokenSource = new CancellationTokenSource();
        if (cancelBeforeDelay) {
            await cancellationTokenSource.CancelAsync();
        }

        bool delayed = await InvokeDelayBeforeRetryAsync(service, cancellationTokenSource.Token);

        Assert.Equal(!cancelBeforeDelay, delayed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RabbitMqConsumer_WhenStoppingRunConsumer_CatchesCancellationAndCancelsOnlyOpenChannel(bool channelIsOpen) {
        var store = new RecordingQueueStore();
        var service = new RabbitMqMailRelayConsumerHostedService(
            Options.Create(CreateRabbitOptions()),
            CreateBroker(new MailRelayBrokerOptions { Backend = MailRelayBrokerOptions.PostgresPollingBackend }),
            store,
            CreateProcessor(store),
            NullLogger<RabbitMqMailRelayConsumerHostedService>.Instance);
        var channelProxy = RabbitMqChannelProxy.Create(channelIsOpen);
        using var cancellationTokenSource = new CancellationTokenSource();

        Task runTask = InvokeRunConsumerAsync(service, channelProxy.Channel, cancellationTokenSource.Token);
        await channelProxy.WaitForConsumeAsync();
        await cancellationTokenSource.CancelAsync();
        await runTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(channelProxy.QosConfigured);
        Assert.True(channelProxy.Consumed);
        Assert.Equal(channelIsOpen, channelProxy.Canceled);
        if (channelIsOpen) {
            Assert.Equal("consumer-tag", channelProxy.CanceledConsumerTag);
        }
    }

    private static RabbitMqMailRelayBroker CreateBroker(MailRelayBrokerOptions options) =>
        new(Options.Create(options), NullLogger<RabbitMqMailRelayBroker>.Instance);

    private static MailRelayBrokerOptions CreateRabbitOptions(int port = 5672, int connectionRetryDelaySeconds = 5) =>
        new() {
            Backend = MailRelayBrokerOptions.RabbitMqBackend,
            HostName = "127.0.0.1",
            Port = port,
            UserName = "guest",
            Password = "guest",
            ConnectionRetryDelaySeconds = connectionRetryDelaySeconds,
        };

    private static MailRelayMessageProcessor CreateProcessor(IMailRelayQueueStore store) =>
        new(
            store,
            new SmtpSubmissionService(new NoopRelayDeliveryTransport()),
            NullLogger<MailRelayMessageProcessor>.Instance);

    private static async Task InvokeExecuteAsync(BackgroundService service, CancellationToken cancellationToken) {
        MethodInfo method = service.GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [cancellationToken])!).ConfigureAwait(false);
    }

    private static async Task InvokeHandleDeliveryAsync(
        RabbitMqMailRelayConsumerHostedService service,
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken) {
        MethodInfo method = service.GetType().GetMethod("HandleDeliveryAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [channel, eventArgs, cancellationToken])!).ConfigureAwait(false);
    }

    private static async Task InvokeRunConsumerAsync(
        RabbitMqMailRelayConsumerHostedService service,
        IChannel channel,
        CancellationToken cancellationToken) {
        MethodInfo method = service.GetType().GetMethod("RunConsumerAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((Task)method.Invoke(service, [channel, cancellationToken])!).ConfigureAwait(false);
    }

    private static BasicDeliverEventArgs CreateDelivery(Guid emailId) =>
        CreateDelivery(emailId.ToString("D"));

    private static BasicDeliverEventArgs CreateDelivery(string body) =>
        new(
            consumerTag: "consumer",
            deliveryTag: 42,
            redelivered: false,
            exchange: string.Empty,
            routingKey: string.Empty,
            properties: new BasicProperties(),
            body: Encoding.UTF8.GetBytes(body));

    private static QueuedEmailMessage CreateQueuedMessage() =>
        new(
            Guid.NewGuid(),
            "sender@example.com",
            "Sender",
            ["recipient@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello",
            CorrelationId: null,
            AttemptCount: 1,
            MaxAttempts: 3);

    private static async Task<bool> InvokeDelayBeforeRetryAsync(object service, CancellationToken cancellationToken) {
        MethodInfo method = service.GetType().GetMethod("DelayBeforeRetryAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return await ((Task<bool>)method.Invoke(service, [cancellationToken])!).ConfigureAwait(false);
    }

    private sealed class NoopRelayDeliveryTransport : IRelayDeliveryTransport {
        public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        private readonly TaskCompletionSource _queueClaimed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _outboxClaimed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _outboxFailed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Exception? ClaimQueueException { get; init; }
        public Exception? ClaimOutboxException { get; init; }
        public bool ThrowCancellationOnQueueClaim { get; init; }
        public bool ThrowCancellationOnOutboxClaim { get; init; }
        public bool ThrowCancellationOnMessageClaim { get; init; }
        public QueuedEmailMessage? ClaimedMessage { get; init; }
        public IReadOnlyList<string> SuppressedRecipients { get; init; } = [];
        public IReadOnlyList<MailRelayOutboxMessage> OutboxBatch { get; init; } = [];
        public bool QueueClaimed { get; private set; }
        public bool OutboxClaimed { get; private set; }
        public bool MessageClaimed { get; private set; }
        public Guid? ClaimedMessageId { get; private set; }
        public Guid? SentMessageId { get; private set; }
        public Guid? SuppressedMessageId { get; private set; }
        public int? FailedOutboxAttemptCount { get; private set; }
        public string FailedOutboxError { get; private set; } = string.Empty;

        public Task WaitForQueueClaimAsync() => _queueClaimed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public Task WaitForOutboxClaimAsync() => _outboxClaimed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public Task WaitForOutboxFailureAsync() => _outboxFailed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        public Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task<IReadOnlyList<QueuedEmailMessage>> ClaimDueBatchAsync(CancellationToken cancellationToken) {
            QueueClaimed = true;
            _queueClaimed.TrySetResult();
            if (ThrowCancellationOnQueueClaim) {
                return Task.FromException<IReadOnlyList<QueuedEmailMessage>>(new OperationCanceledException(cancellationToken));
            }

            return ClaimQueueException is null
                ? Task.FromResult<IReadOnlyList<QueuedEmailMessage>>([])
                : Task.FromException<IReadOnlyList<QueuedEmailMessage>>(ClaimQueueException);
        }

        public Task<QueuedEmailMessage?> TryClaimMessageByIdAsync(Guid id, CancellationToken cancellationToken) {
            MessageClaimed = true;
            ClaimedMessageId = id;
            return ThrowCancellationOnMessageClaim
                ? Task.FromException<QueuedEmailMessage?>(new OperationCanceledException(cancellationToken))
                : Task.FromResult(ClaimedMessage);
        }

        public Task<IReadOnlyList<MailRelayOutboxMessage>> ClaimOutboxBatchAsync(CancellationToken cancellationToken) {
            OutboxClaimed = true;
            _outboxClaimed.TrySetResult();
            if (ThrowCancellationOnOutboxClaim) {
                return Task.FromException<IReadOnlyList<MailRelayOutboxMessage>>(new OperationCanceledException(cancellationToken));
            }

            return ClaimOutboxException is null
                ? Task.FromResult(OutboxBatch)
                : Task.FromException<IReadOnlyList<MailRelayOutboxMessage>>(ClaimOutboxException);
        }

        public Task MarkOutboxPublishedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkOutboxFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken) {
            FailedOutboxAttemptCount = attemptCount;
            FailedOutboxError = error;
            _outboxFailed.TrySetResult();
            return Task.CompletedTask;
        }

        public Task<MailRelayInboxClaimResult> TryClaimInboxMessageAsync(
            string consumerName,
            string messageKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MailRelayInboxClaimResult(Claimed: true, Guid.NewGuid()));

        public Task MarkInboxProcessedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkInboxFailedAsync(Guid id, string error, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkSentAsync(Guid id, CancellationToken cancellationToken) {
            SentMessageId = id;
            return Task.CompletedTask;
        }

        public Task MarkSuppressedAsync(Guid id, IReadOnlyCollection<string> recipients, CancellationToken cancellationToken) {
            SuppressedMessageId = id;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(string? email, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MailRelaySuppressionEntry>>([]);

        public Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(
            IngestMailEventRequest request,
            CancellationToken cancellationToken) =>
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

        public Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
            IReadOnlyCollection<string> recipients,
            CancellationToken cancellationToken) =>
            Task.FromResult(SuppressedRecipients);

        public Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new MailRelayQueueStats(0, 0, 0, 0, 0, 0));

        public Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<MailRelayMessageDetails?>(null);

        public Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(null);
    }

    private class RabbitMqChannelProxy : DispatchProxy {
        private readonly TaskCompletionSource _consumed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IChannel Channel => (IChannel)(object)this;
        public bool IsOpen { get; private set; } = true;
        public bool QosConfigured { get; private set; }
        public bool Consumed { get; private set; }
        public bool Canceled { get; private set; }
        public string? CanceledConsumerTag { get; private set; }
        public bool Acked { get; private set; }
        public ulong AckedDeliveryTag { get; private set; }
        public bool Nacked { get; private set; }
        public ulong NackedDeliveryTag { get; private set; }
        public bool NackRequeue { get; private set; }

        public static RabbitMqChannelProxy Create(bool isOpen = true) {
            object proxy = Create<IChannel, RabbitMqChannelProxy>();
            var channelProxy = (RabbitMqChannelProxy)proxy;
            channelProxy.IsOpen = isOpen;
            return channelProxy;
        }

        public Task WaitForConsumeAsync() => _consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (string.Equals(targetMethod.Name, "get_" + nameof(IChannel.IsOpen), StringComparison.Ordinal)) {
                return IsOpen;
            }

            if (string.Equals(targetMethod.Name, nameof(IChannel.BasicQosAsync), StringComparison.Ordinal)) {
                QosConfigured = true;
                return CreateCompletedAsyncResult(targetMethod);
            }

            if (string.Equals(targetMethod.Name, nameof(IChannel.BasicConsumeAsync), StringComparison.Ordinal)) {
                Consumed = true;
                _consumed.TrySetResult();
                if (targetMethod.ReturnType == typeof(ValueTask<string>)) {
                    return new ValueTask<string>("consumer-tag");
                }

                return Task.FromResult("consumer-tag");
            }

            if (string.Equals(targetMethod.Name, nameof(IChannel.BasicCancelAsync), StringComparison.Ordinal)) {
                Canceled = true;
                CanceledConsumerTag = (string)args![0]!;
                return CreateCompletedAsyncResult(targetMethod);
            }

            if (string.Equals(targetMethod.Name, nameof(IChannel.BasicAckAsync), StringComparison.Ordinal)) {
                Acked = true;
                AckedDeliveryTag = (ulong)args![0]!;
                return CreateCompletedAsyncResult(targetMethod);
            }

            if (string.Equals(targetMethod.Name, nameof(IChannel.BasicNackAsync), StringComparison.Ordinal)) {
                Nacked = true;
                NackedDeliveryTag = (ulong)args![0]!;
                NackRequeue = (bool)args[2]!;
                return CreateCompletedAsyncResult(targetMethod);
            }

            if (targetMethod.ReturnType == typeof(ValueTask)) {
                return new ValueTask();
            }

            if (targetMethod.ReturnType == typeof(ValueTask<bool>)) {
                return new ValueTask<bool>(result: false);
            }

            if (targetMethod.ReturnType == typeof(Task)) {
                return Task.CompletedTask;
            }

            return targetMethod.ReturnType.IsValueType
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }

        private static object CreateCompletedAsyncResult(MethodInfo targetMethod) =>
            targetMethod.ReturnType == typeof(ValueTask)
                ? new ValueTask()
                : Task.CompletedTask;
    }
}
