using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayMessageProcessorTests {
    [Fact]
    public async Task ProcessAsync_WhenRecipientIsSuppressed_MarksMessageSuppressedAndDoesNotSend() {
        var store = new RecordingQueueStore {
            SuppressedRecipients = ["user@example.com"]
        };
        var transport = new RecordingTransport();
        var processor = CreateProcessor(store, transport);

        var result = await processor.ProcessAsync(CreateMessage(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.True(result.IsTerminalFailure);
        Assert.Equal(QueuedEmailStatus.Suppressed, store.Status);
        Assert.False(transport.SendCalled);
    }

    [Fact]
    public async Task ProcessAsync_WhenTransportSucceeds_MarksMessageSent() {
        var store = new RecordingQueueStore();
        var transport = new RecordingTransport();
        var processor = CreateProcessor(store, transport);

        var result = await processor.ProcessAsync(CreateMessage(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.False(result.IsTerminalFailure);
        Assert.Equal(QueuedEmailStatus.Sent, store.Status);
        Assert.True(transport.SendCalled);
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
            Exception = new InvalidOperationException("SMTP failed")
        };
        var processor = CreateProcessor(store, transport);

        var result = await processor.ProcessAsync(CreateMessage(attemptCount, maxAttempts), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(expectedTerminalFailure, result.IsTerminalFailure);
        Assert.Equal(expectedStatus, store.FailureDecision?.Status);
        Assert.Equal(attemptCount, store.FailureDecision?.AttemptCount);
    }

    private static MailRelayMessageProcessor CreateProcessor(RecordingQueueStore store, RecordingTransport transport) =>
        new(
            store,
            new SmtpSubmissionService(transport),
            NullLogger<MailRelayMessageProcessor>.Instance);

    private static QueuedEmailMessage CreateMessage(int attemptCount = 1, int maxAttempts = 3) =>
        new(
            Guid.NewGuid(),
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello",
            "correlation",
            attemptCount,
            maxAttempts);

    private sealed class RecordingTransport : IRelayDeliveryTransport {
        public bool SendCalled { get; private set; }
        public Exception? Exception { get; init; }

        public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
            SendCalled = true;
            return Exception is null ? Task.CompletedTask : Task.FromException(Exception);
        }
    }

    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        public IReadOnlyList<string> SuppressedRecipients { get; init; } = [];
        public string? Status { get; private set; }
        public QueuedEmailFailureDecision? FailureDecision { get; private set; }

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
            Task.FromResult(new MailRelayInboxClaimResult(true, Guid.NewGuid()));

        public Task MarkInboxProcessedAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkInboxFailedAsync(Guid id, string error, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkSentAsync(Guid id, CancellationToken cancellationToken) {
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

        public Task MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) {
            FailureDecision = decision;
            Status = decision.Status;
            return Task.CompletedTask;
        }
    }
}
