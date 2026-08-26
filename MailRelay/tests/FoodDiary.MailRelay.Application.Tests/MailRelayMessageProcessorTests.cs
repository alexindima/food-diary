using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailRelay.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayMessageProcessorTests {
    [Fact]
    public async Task ProcessAsync_WhenRecipientIsSuppressed_MarksMessageSuppressedAndDoesNotSend() {
        var store = new RecordingQueueStore {
            SuppressedRecipients = ["user@example.com"],
        };
        var transport = new RecordingTransport();
        var logger = new RecordingLogger();
        MailRelayMessageProcessor processor = CreateProcessor(store, transport, logger);

        MailRelayProcessResult result = await processor.ProcessAsync(CreateMessage(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.IsTerminalFailure);
        Assert.Equal(QueuedEmailStatus.Suppressed, store.Status);
        Assert.False(transport.SendCalled);
        Assert.DoesNotContain("user@example.com", string.Join(Environment.NewLine, logger.Messages), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_WhenTransportSucceeds_MarksMessageSent() {
        var store = new RecordingQueueStore();
        var transport = new RecordingTransport();
        MailRelayMessageProcessor processor = CreateProcessor(store, transport);

        MailRelayProcessResult result = await processor.ProcessAsync(CreateMessage(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.IsTerminalFailure);
        Assert.Equal(QueuedEmailStatus.Sent, store.Status);
        Assert.True(transport.SendCalled);
    }

    [Fact]
    public async Task ProcessAsync_WhenSameQueuedEmailIsRetried_ReusesTransportMessageId() {
        var store = new RecordingQueueStore();
        var transport = new RecordingTransport();
        MailRelayMessageProcessor processor = CreateProcessor(store, transport);
        var messageId = Guid.NewGuid();

        await processor.ProcessAsync(CreateMessage(messageId, attemptCount: 1), CancellationToken.None);
        await processor.ProcessAsync(CreateMessage(messageId, attemptCount: 2), CancellationToken.None);

        Assert.Equal(2, transport.Requests.Count);
        Assert.All(transport.Requests, request =>
            Assert.Equal($"{messageId:N}@mailrelay.invalid", request.MessageId));
    }

    [Fact]
    public async Task ProcessAsync_WhenShutdownStartsAfterSmtpAcceptance_FinalizesSentStateWithoutCancellation() {
        var store = new RecordingQueueStore();
        using var cancellationTokenSource = new CancellationTokenSource();
        var transport = new RecordingTransport {
            AfterSend = cancellationTokenSource.Cancel,
        };
        MailRelayMessageProcessor processor = CreateProcessor(store, transport);

        MailRelayProcessResult result = await processor.ProcessAsync(CreateMessage(), cancellationTokenSource.Token);

        Assert.True(result.Succeeded);
        Assert.Equal(QueuedEmailStatus.Sent, store.Status);
        Assert.False(store.MarkSentCancellationToken.CanBeCanceled);
    }

    [Theory]
    [InlineData(1, 3, QueuedEmailStatus.Retry, false)]
    [InlineData(3, 3, QueuedEmailStatus.Failed, true)]
    public async Task ProcessAsync_WhenTransportFails_MarksFailureDecision(
        int attemptCount,
        int maxAttempts,
        string expectedStatus,
        bool expectedTerminalFailure) {
        var store = new RecordingQueueStore();
        var transport = new RecordingTransport {
            Exception = new InvalidOperationException("SMTP failed"),
        };
        MailRelayMessageProcessor processor = CreateProcessor(store, transport);

        MailRelayProcessResult result = await processor.ProcessAsync(CreateMessage(attemptCount, maxAttempts), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(expectedTerminalFailure, result.IsTerminalFailure);
        Assert.Equal(expectedStatus, store.FailureDecision?.Status);
        Assert.Equal(attemptCount, store.FailureDecision?.AttemptCount);
        Assert.Equal("Delivery failed (InvalidOperationException).", store.FailureDecision?.Error);
    }

    [Fact]
    public async Task ProcessAsync_WhenCancellationIsRequested_RethrowsOperationCanceledException() {
        var store = new RecordingQueueStore();
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        var transport = new RecordingTransport {
            Exception = new OperationCanceledException(cancellationTokenSource.Token),
        };
        MailRelayMessageProcessor processor = CreateProcessor(store, transport);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            processor.ProcessAsync(CreateMessage(), cancellationTokenSource.Token));

        Assert.Null(store.FailureDecision);
    }

    [Fact]
    public async Task ProcessAsync_WhenTransportExceptionContainsRecipient_DoesNotLogExceptionPayload() {
        var store = new RecordingQueueStore();
        var transport = new RecordingTransport {
            Exception = new InvalidOperationException("SMTP rejected user@example.com"),
        };
        var logger = new RecordingLogger();
        MailRelayMessageProcessor processor = CreateProcessor(store, transport, logger);

        await processor.ProcessAsync(CreateMessage(), CancellationToken.None);

        Assert.DoesNotContain("user@example.com", string.Join(Environment.NewLine, logger.Messages), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correlation", string.Join(Environment.NewLine, logger.Messages), StringComparison.OrdinalIgnoreCase);
        Assert.All(logger.Exceptions, Assert.Null);
        Assert.Equal("Delivery failed (InvalidOperationException).", store.FailureDecision?.Error);
    }

    private static MailRelayMessageProcessor CreateProcessor(
        RecordingQueueStore store,
        RecordingTransport transport,
        ILogger<MailRelayMessageProcessor>? logger = null) =>
        new(
            store,
            new SmtpSubmissionService(transport),
            logger ?? NullLogger<MailRelayMessageProcessor>.Instance);

    private static QueuedEmailMessage CreateMessage(int attemptCount = 1, int maxAttempts = 3) =>
        CreateMessage(Guid.NewGuid(), attemptCount, maxAttempts);

    private static QueuedEmailMessage CreateMessage(Guid id, int attemptCount = 1, int maxAttempts = 3) =>
        new(
            id,
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello",
            "correlation",
            attemptCount,
            maxAttempts);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingTransport : IRelayDeliveryTransport {
        public bool SendCalled { get; private set; }
        public Exception? Exception { get; init; }
        public Action? AfterSend { get; init; }
        public List<RelayEmailMessageRequest> Requests { get; } = [];

        public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
            SendCalled = true;
            Requests.Add(request);
            AfterSend?.Invoke();
            return Exception is null ? Task.CompletedTask : Task.FromException(Exception);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        public IReadOnlyList<string> SuppressedRecipients { get; init; } = [];
        public string? Status { get; private set; }
        public QueuedEmailFailureDecision? FailureDecision { get; private set; }
        public CancellationToken MarkSentCancellationToken { get; private set; }

        public Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Guid.NewGuid());

        public Task<IReadOnlyList<QueuedEmailMessage>> ClaimDueBatchAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QueuedEmailMessage>>([]);

        public Task<QueuedEmailMessage?> TryClaimMessageByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<QueuedEmailMessage?>(null);

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

        public Task MarkSentAsync(Guid id, CancellationToken cancellationToken) {
            MarkSentCancellationToken = cancellationToken;
            Status = QueuedEmailStatus.Sent;
            return Task.CompletedTask;
        }

        public Task MarkSuppressedAsync(Guid id, IReadOnlyCollection<string> recipients, CancellationToken cancellationToken) {
            Status = QueuedEmailStatus.Suppressed;
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

        public Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) {
            FailureDecision = decision;
            Status = decision.Status;
            return Task.FromResult<DateTimeOffset?>(decision.IsTerminalFailure ? null : DateTimeOffset.UtcNow.AddSeconds(1));
        }
    }

    private sealed class RecordingLogger : ILogger<MailRelayMessageProcessor> {
        public List<string> Messages { get; } = [];
        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }
}
