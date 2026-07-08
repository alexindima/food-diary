using FoodDiary.MailRelay.Application;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.MailRelay.Application.Emails.Commands;
using FoodDiary.MailRelay.Application.Emails.Queries;
using FoodDiary.MailRelay.Application.Health;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.MailRelay.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailRelayApplicationValidationTests {
    [Fact]
    public async Task EnqueueCommand_WhenRequestIsInvalid_ReturnsValidationFailureWithoutCallingStore() {
        var queueStore = new RecordingQueueStore();
        await using ServiceProvider provider = CreateProvider(queueStore);
        ISender sender = provider.GetRequiredService<ISender>();

        Result<Guid> result = await sender.Send(new EnqueueMailRelayEmailCommand(new RelayEmailMessageRequest(
            "",
            "",
            [],
            "",
            "",
            TextBody: null)));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.False(queueStore.EnqueueCalled);
    }

    [Fact]
    public async Task IngestDeliveryEventCommand_WhenEventTypeIsInvalid_ReturnsValidationFailure() {
        await using ServiceProvider provider = CreateProvider(new RecordingQueueStore());
        ISender sender = provider.GetRequiredService<ISender>();

        Result<MailRelayDeliveryEventEntry> result = await sender.Send(new IngestMailRelayDeliveryEventCommand(new IngestMailEventRequest(
            "opened",
            "user@example.com",
            "test")));

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation.Invalid", result.Error?.Code);
    }

    [Fact]
    public async Task IngestManyDeliveryEventsCommand_WhenRequestsAreValid_DelegatesToIngestionService() {
        var queueStore = new RecordingQueueStore();
        await using ServiceProvider provider = CreateProvider(queueStore);
        ISender sender = provider.GetRequiredService<ISender>();

        Result<IReadOnlyList<MailRelayDeliveryEventEntry>> result = await sender.Send(new IngestManyMailRelayDeliveryEventsCommand([
            new IngestMailEventRequest(MailRelayDeliveryEventType.Complaint, "user@example.com", "test"),
        ]));

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Single(queueStore.RecordedEvents);
    }

    [Fact]
    public void IngestManyDeliveryEventsValidator_RejectsEmptyAndInvalidItems() {
        var validator = new IngestManyMailRelayDeliveryEventsCommandValidator();

        FluentValidation.Results.ValidationResult emptyResult = validator.Validate(new IngestManyMailRelayDeliveryEventsCommand([]));
        FluentValidation.Results.ValidationResult invalidItemResult = validator.Validate(new IngestManyMailRelayDeliveryEventsCommand([
            new IngestMailEventRequest("", "not-an-email", "", "unknown"),
        ]));

        Assert.False(emptyResult.IsValid);
        Assert.False(invalidItemResult.IsValid);
        Assert.Contains(invalidItemResult.Errors, static failure => failure.PropertyName.Contains("EventType", StringComparison.Ordinal));
        Assert.Contains(invalidItemResult.Errors, static failure => failure.PropertyName.Contains("Email", StringComparison.Ordinal));
        Assert.Contains(invalidItemResult.Errors, static failure => failure.PropertyName.Contains("Source", StringComparison.Ordinal));
        Assert.Contains(invalidItemResult.Errors, static failure => failure.PropertyName.Contains("Classification", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetSuppressionsQuery_WhenEmailIsInvalid_ReturnsValidationFailure() {
        await using ServiceProvider provider = CreateProvider(new RecordingQueueStore());
        ISender sender = provider.GetRequiredService<ISender>();

        Result<IReadOnlyList<MailRelaySuppressionEntry>> result = await sender.Send(new GetMailRelaySuppressionsQuery("not-an-email"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
    }

    [Fact]
    public void RemoveSuppressionValidator_RejectsMissingOrInvalidEmail() {
        var validator = new RemoveMailRelaySuppressionCommandValidator();

        Assert.False(validator.Validate(new RemoveMailRelaySuppressionCommand("")).IsValid);
        Assert.False(validator.Validate(new RemoveMailRelaySuppressionCommand("not-an-email")).IsValid);
        Assert.True(validator.Validate(new RemoveMailRelaySuppressionCommand("user@example.com")).IsValid);
    }

    [Fact]
    public void CreateSuppressionValidator_UsesTimeProviderForExpiresAtValidation() {
        var validator = new CreateMailRelaySuppressionCommandValidator(FixedTime);

        FluentValidation.Results.ValidationResult pastResult = validator.Validate(new CreateMailRelaySuppressionCommand(
            new CreateSuppressionRequest("user@example.com", "manual", "test", FixedNow.AddMinutes(-1))));
        FluentValidation.Results.ValidationResult futureResult = validator.Validate(new CreateMailRelaySuppressionCommand(
            new CreateSuppressionRequest("user@example.com", "manual", "test", FixedNow.AddMinutes(1))));

        Assert.False(pastResult.IsValid);
        Assert.True(futureResult.IsValid);
    }

    [Fact]
    public async Task CheckReadinessQueryHandler_WhenCheckerSucceeds_ReturnsSuccess() {
        var checker = new RecordingReadinessChecker();
        var handler = new CheckMailRelayReadinessQueryHandler(checker);

        Result result = await handler.Handle(new CheckMailRelayReadinessQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(checker.Called);
    }

    private static ServiceProvider CreateProvider(RecordingQueueStore queueStore) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMailRelayQueueStore>(queueStore);
        services.AddSingleton<IMailRelayDispatchNotifier, NoOpDispatchNotifier>();
        services.AddMailRelayApplication();
        return services.BuildServiceProvider();
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoOpDispatchNotifier : IMailRelayDispatchNotifier {
        public Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingReadinessChecker : IMailRelayReadinessChecker {
        public bool Called { get; private set; }

        public Task CheckReadyAsync(CancellationToken cancellationToken) {
            Called = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        public bool EnqueueCalled { get; private set; }
        public List<IngestMailEventRequest> RecordedEvents { get; } = [];

        public Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
            EnqueueCalled = true;
            return Task.FromResult(Guid.NewGuid());
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

        public Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(string? email, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<MailRelaySuppressionEntry>>([]);

        public Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

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
            Task.FromResult<IReadOnlyList<MailRelayDeliveryEventEntry>>([]);

        public Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
            IReadOnlyCollection<string> recipients,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new MailRelayQueueStats(0, 0, 0, 0, 0, 0));

        public Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<MailRelayMessageDetails?>(null);

        public Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) =>
            Task.FromResult<DateTimeOffset?>(decision.IsTerminalFailure ? null : DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
