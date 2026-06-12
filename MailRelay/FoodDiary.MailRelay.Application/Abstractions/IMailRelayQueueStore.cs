namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelayQueueStore {
    Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<QueuedEmailMessage>> ClaimDueBatchAsync(CancellationToken cancellationToken);
    Task<QueuedEmailMessage?> TryClaimMessageByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MailRelayOutboxMessage>> ClaimOutboxBatchAsync(CancellationToken cancellationToken);
    Task MarkOutboxPublishedAsync(Guid id, CancellationToken cancellationToken);
    Task MarkOutboxFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken);
    Task<MailRelayInboxClaimResult> TryClaimInboxMessageAsync(
        string consumerName,
        string messageKey,
        CancellationToken cancellationToken);
    Task MarkInboxProcessedAsync(Guid id, CancellationToken cancellationToken);
    Task MarkInboxFailedAsync(Guid id, string error, CancellationToken cancellationToken);
    Task MarkSentAsync(Guid id, CancellationToken cancellationToken);
    Task MarkSuppressedAsync(Guid id, IReadOnlyCollection<string> recipients, CancellationToken cancellationToken);
    Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(string? email, CancellationToken cancellationToken);
    Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken);
    Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MailRelayDeliveryEventEntry>> GetDeliveryEventsAsync(
        string? email,
        CancellationToken cancellationToken);
    Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken);
    Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken);
    Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken);
}
