using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Common.Results;
using FoodDiary.MailRelay.Application.DeliveryEvents.Services;
using FoodDiary.MailRelay.Application.Emails.Commands;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Queries;
using FoodDiary.MailRelay.Application.Emails.Services;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayApplicationFeatureTests {
    [Fact]
    public async Task EnqueueAsync_StoresMessageAndNotifiesDispatcher() {
        var store = new RecordingQueueStore { QueuedEmailId = Guid.NewGuid() };
        var notifier = new RecordingDispatchNotifier();
        var useCases = new MailRelayEmailUseCases(store, notifier, new NoOpMailRelayDeliveryPolicy());
        RelayEmailMessageRequest request = CreateRelayRequest();

        Result<Guid> result = await useCases.EnqueueAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(store.QueuedEmailId, result.Value);
        Assert.Same(request, store.EnqueueRequest);
        Assert.Equal(store.QueuedEmailId, notifier.NotifiedQueuedEmailId);
    }

    [Fact]
    public async Task EnqueueAsync_WhenDeliveryPolicyRejectsRequest_DoesNotStoreOrNotify() {
        var store = new RecordingQueueStore { QueuedEmailId = Guid.NewGuid() };
        var notifier = new RecordingDispatchNotifier();
        var useCases = new MailRelayEmailUseCases(
            store,
            notifier,
            new RejectingMailRelayDeliveryPolicy());

        Result<Guid> result = await useCases.EnqueueAsync(CreateRelayRequest(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.False(store.EnqueueCalled);
        Assert.Null(notifier.NotifiedQueuedEmailId);
    }

    [Fact]
    public async Task RemoveSuppressionHandler_WhenSuppressionDoesNotExist_ReturnsNotFound() {
        var handler = new RemoveMailRelaySuppressionCommandHandler(CreateUseCases(new RecordingQueueStore {
            RemoveSuppressionResult = false,
        }));

        Result result = await handler.Handle(new RemoveMailRelaySuppressionCommand("missing@example.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorKind.NotFound, result.Error?.Kind);
    }

    [Fact]
    public async Task RemoveSuppressionHandler_WhenSuppressionExists_ReturnsSuccess() {
        var handler = new RemoveMailRelaySuppressionCommandHandler(CreateUseCases(new RecordingQueueStore {
            RemoveSuppressionResult = true,
        }));

        Result result = await handler.Handle(new RemoveMailRelaySuppressionCommand("known@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetMessageDetailsHandler_WhenMessageDoesNotExist_ReturnsNotFound() {
        var id = Guid.NewGuid();
        var handler = new GetMailRelayMessageDetailsQueryHandler(CreateUseCases(new RecordingQueueStore()));

        Result<MailRelayMessageDetails> result = await handler.Handle(new GetMailRelayMessageDetailsQuery(id), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("MailRelay.Message.NotFound", result.Error?.Code);
    }

    [Fact]
    public async Task GetMessageDetailsHandler_WhenMessageExists_ReturnsDetails() {
        var details = new MailRelayMessageDetails(
            Guid.NewGuid(),
            QueuedEmailStatus.Sent,
            "Subject",
            "correlation",
            1,
            3,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            LockedAtUtc: null,
            DateTimeOffset.UtcNow,
            LastError: null,
            SuppressedRecipients: null);
        var handler = new GetMailRelayMessageDetailsQueryHandler(CreateUseCases(new RecordingQueueStore {
            MessageDetails = details,
        }));

        Result<MailRelayMessageDetails> result = await handler.Handle(new GetMailRelayMessageDetailsQuery(details.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(details, result.Value);
    }

    [Fact]
    public async Task QueryHandlers_ReturnQueueStateFromUseCases() {
        var suppression = new MailRelaySuppressionEntry(
            "user@example.com",
            "manual",
            "test",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            ExpiresAtUtc: null);
        var deliveryEvent = new MailRelayDeliveryEventEntry(
            Guid.NewGuid(),
            MailRelayDeliveryEventType.Complaint,
            "user@example.com",
            "test",
            Classification: null,
            ProviderMessageId: null,
            "complaint",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var stats = new MailRelayQueueStats(1, 2, 3, 4, 5, 6);
        MailRelayEmailUseCases useCases = CreateUseCases(new RecordingQueueStore {
            Suppressions = [suppression],
            DeliveryEvents = [deliveryEvent],
            Stats = stats,
        });

        Result<IReadOnlyList<MailRelaySuppressionEntry>> suppressions = await new GetMailRelaySuppressionsQueryHandler(useCases)
            .Handle(new GetMailRelaySuppressionsQuery("user@example.com"), CancellationToken.None);
        Result<IReadOnlyList<MailRelayDeliveryEventEntry>> deliveryEvents = await new GetMailRelayDeliveryEventsQueryHandler(useCases)
            .Handle(new GetMailRelayDeliveryEventsQuery("user@example.com"), CancellationToken.None);
        Result<MailRelayQueueStats> queueStats = await new GetMailRelayQueueStatsQueryHandler(useCases)
            .Handle(new GetMailRelayQueueStatsQuery(), CancellationToken.None);

        Assert.Same(suppression, Assert.Single(suppressions.Value));
        Assert.Same(deliveryEvent, Assert.Single(deliveryEvents.Value));
        Assert.Same(stats, queueStats.Value);
    }

    [Fact]
    public async Task CommandHandlers_DelegateToUseCases() {
        var store = new RecordingQueueStore { QueuedEmailId = Guid.NewGuid() };
        MailRelayEmailUseCases useCases = CreateUseCases(store);
        RelayEmailMessageRequest request = CreateRelayRequest();
        var suppressionRequest = new CreateSuppressionRequest("user@example.com", "manual", "test");

        Result<Guid> enqueueResult = await new EnqueueMailRelayEmailCommandHandler(useCases)
            .Handle(new EnqueueMailRelayEmailCommand(request), CancellationToken.None);
        Result createResult = await new CreateMailRelaySuppressionCommandHandler(useCases)
            .Handle(new CreateMailRelaySuppressionCommand(suppressionRequest), CancellationToken.None);

        Assert.Equal(store.QueuedEmailId, enqueueResult.Value);
        Assert.True(createResult.IsSuccess);
        Assert.Same(suppressionRequest, store.UpsertSuppressionRequest);
    }

    [Fact]
    public async Task IngestionService_WhenEventTypeIsInvalid_ReturnsFailureWithoutRecordingEvent() {
        var store = new RecordingQueueStore();
        var service = new MailRelayDeliveryEventIngestionService(store);

        Result<MailRelayDeliveryEventEntry> result = await service.IngestAsync(
            new IngestMailEventRequest("opened", "user@example.com", "test"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("MailRelay.DeliveryEvent.InvalidEventType", result.Error?.Code);
        Assert.Empty(store.RecordedEvents);
    }

    [Fact]
    public async Task IngestionService_WhenHardBounceIsRecorded_CreatesSuppressionWithDefaultReason() {
        var store = new RecordingQueueStore();
        var service = new MailRelayDeliveryEventIngestionService(store);

        Result<MailRelayDeliveryEventEntry> result = await service.IngestAsync(
            new IngestMailEventRequest(
                " Bounce ",
                "user@example.com",
                "mailgun",
                Classification: MailRelayBounceClassification.Hard),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        IngestMailEventRequest recorded = Assert.Single(store.RecordedEvents);
        Assert.Equal(MailRelayDeliveryEventType.Bounce, recorded.EventType);
        CreateSuppressionRequest suppression = Assert.Single(store.UpsertSuppressions);
        Assert.Equal("user@example.com", suppression.Email);
        Assert.Equal(MailRelaySuppressionPolicy.HardBounceReason, suppression.Reason);
    }

    [Fact]
    public async Task IngestionService_WhenSoftBounceIsRecorded_DoesNotCreateSuppression() {
        var store = new RecordingQueueStore();
        var service = new MailRelayDeliveryEventIngestionService(store);

        Result<MailRelayDeliveryEventEntry> result = await service.IngestAsync(
            new IngestMailEventRequest(
                MailRelayDeliveryEventType.Bounce,
                "user@example.com",
                "ses",
                Classification: MailRelayBounceClassification.Soft),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(store.UpsertSuppressions);
    }

    [Fact]
    public async Task IngestionService_WhenBatchContainsInvalidEvent_StopsAndReturnsFailure() {
        var store = new RecordingQueueStore();
        var service = new MailRelayDeliveryEventIngestionService(store);

        Result<IReadOnlyList<MailRelayDeliveryEventEntry>> result = await service.IngestManyAsync(
            [
                new IngestMailEventRequest(MailRelayDeliveryEventType.Complaint, "first@example.com", "ses"),
                new IngestMailEventRequest("opened", "second@example.com", "ses"),
            ],
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(store.RecordedEvents);
    }

    private static MailRelayEmailUseCases CreateUseCases(RecordingQueueStore store) =>
        new(store, new RecordingDispatchNotifier(), new NoOpMailRelayDeliveryPolicy());

    private static RelayEmailMessageRequest CreateRelayRequest() =>
        new(
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Body</p>",
            "Body",
            "correlation");

    [ExcludeFromCodeCoverage]
    private sealed class RecordingDispatchNotifier : IMailRelayDispatchNotifier {
        public Guid? NotifiedQueuedEmailId { get; private set; }

        public Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken) {
            NotifiedQueuedEmailId = queuedEmailId;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RejectingMailRelayDeliveryPolicy : IMailRelayDeliveryPolicy {
        public Result CanEnqueue(RelayEmailMessageRequest request) =>
            Result.Failure(MailRelayErrors.DirectMxRequiresSingleRecipientDomain());
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        public Guid QueuedEmailId { get; init; } = Guid.NewGuid();
        public bool EnqueueCalled { get; private set; }
        public RelayEmailMessageRequest? EnqueueRequest { get; private set; }
        public CreateSuppressionRequest? UpsertSuppressionRequest { get; private set; }
        public List<CreateSuppressionRequest> UpsertSuppressions { get; } = [];
        public List<IngestMailEventRequest> RecordedEvents { get; } = [];
        public bool RemoveSuppressionResult { get; init; }
        public MailRelayMessageDetails? MessageDetails { get; init; }
        public IReadOnlyList<MailRelaySuppressionEntry> Suppressions { get; init; } = [];
        public IReadOnlyList<MailRelayDeliveryEventEntry> DeliveryEvents { get; init; } = [];
        public MailRelayQueueStats Stats { get; init; } = new(0, 0, 0, 0, 0, 0);

        public Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
            EnqueueCalled = true;
            EnqueueRequest = request;
            return Task.FromResult(QueuedEmailId);
        }

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

        public Task MarkSentAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task MarkSuppressedAsync(Guid id, IReadOnlyCollection<string> recipients, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(
            string? email,
            CancellationToken cancellationToken) =>
            Task.FromResult(Suppressions);

        public Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) {
            UpsertSuppressionRequest = request;
            UpsertSuppressions.Add(request);
            return Task.CompletedTask;
        }

        public Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(
            IngestMailEventRequest request,
            CancellationToken cancellationToken) {
            RecordedEvents.Add(request);
            return Task.FromResult(new MailRelayDeliveryEventEntry(
                Guid.NewGuid(),
                request.EventType,
                request.Email,
                request.Source,
                request.Classification,
                request.ProviderMessageId,
                request.Reason,
                request.OccurredAtUtc ?? DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
        }

        public Task<IReadOnlyList<MailRelayDeliveryEventEntry>> GetDeliveryEventsAsync(
            string? email,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeliveryEvents);

        public Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(RemoveSuppressionResult);

        public Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
            IReadOnlyCollection<string> recipients,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Stats);

        public Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(MessageDetails);

        public Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(decision.IsTerminalFailure ? null : DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
