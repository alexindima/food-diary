using FoodDiary.MailRelay.Application;
using FoodDiary.MailRelay.Application.Abstractions;
using FoodDiary.MailRelay.Application.Common.Result;
using FoodDiary.MailRelay.Application.Emails.Commands;
using FoodDiary.MailRelay.Application.Emails.Queries;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Queue.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayApplicationValidationTests {
    [Fact]
    public async Task EnqueueCommand_WhenRequestIsInvalid_ReturnsValidationFailureWithoutCallingStore() {
        var queueStore = new RecordingQueueStore();
        using var provider = CreateProvider(queueStore);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new EnqueueMailRelayEmailCommand(new RelayEmailMessageRequest(
            "",
            "",
            [],
            "",
            "",
            null)));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
        Assert.False(queueStore.EnqueueCalled);
    }

    [Fact]
    public async Task IngestDeliveryEventCommand_WhenEventTypeIsInvalid_ReturnsValidationFailure() {
        using var provider = CreateProvider(new RecordingQueueStore());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new IngestMailRelayDeliveryEventCommand(new IngestMailEventRequest(
            "opened",
            "user@example.com",
            "test")));

        Assert.False(result.IsSuccess);
        Assert.Equal("Validation.Invalid", result.Error?.Code);
    }

    [Fact]
    public async Task GetSuppressionsQuery_WhenEmailIsInvalid_ReturnsValidationFailure() {
        using var provider = CreateProvider(new RecordingQueueStore());
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetMailRelaySuppressionsQuery("not-an-email"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error?.Kind);
    }

    private static ServiceProvider CreateProvider(RecordingQueueStore queueStore) {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMailRelayQueueStore>(queueStore);
        services.AddSingleton<IMailRelayDispatchNotifier, NoOpDispatchNotifier>();
        services.AddMailRelayApplication();
        return services.BuildServiceProvider();
    }

    private sealed class NoOpDispatchNotifier : IMailRelayDispatchNotifier {
        public Task NotifyQueuedAsync(Guid queuedEmailId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RecordingQueueStore : IMailRelayQueueStore {
        public bool EnqueueCalled { get; private set; }

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
            Task.FromResult(new MailRelayInboxClaimResult(true, Guid.NewGuid()));

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

        public Task MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
