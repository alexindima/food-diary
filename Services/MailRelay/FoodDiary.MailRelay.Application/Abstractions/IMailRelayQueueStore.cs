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
    async Task<MailRelayPage<MailRelaySuppressionEntry>> GetSuppressionsPageAsync(string? email, int page, int limit, CancellationToken cancellationToken) {
        int normalizedPage = Math.Clamp(page, 1, 10_000);
        int normalizedLimit = Math.Clamp(limit, 1, 100);
        IReadOnlyList<MailRelaySuppressionEntry> all = await GetSuppressionsAsync(email, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MailRelaySuppressionEntry> data = all.Skip((normalizedPage - 1) * normalizedLimit).Take(normalizedLimit).ToArray();
        int totalPages = all.Count == 0 ? 0 : (int)Math.Ceiling(all.Count / (double)normalizedLimit);
        return new MailRelayPage<MailRelaySuppressionEntry>(data, normalizedPage, normalizedLimit, totalPages, all.Count);
    }
    Task UpsertSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken);
    Task<MailRelayDeliveryEventEntry> RecordDeliveryEventAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MailRelayDeliveryEventEntry>> GetDeliveryEventsAsync(string? email, CancellationToken cancellationToken);
    async Task<MailRelayPage<MailRelayDeliveryEventEntry>> GetDeliveryEventsPageAsync(
        string? email, int page, int limit, CancellationToken cancellationToken) {
        int normalizedPage = Math.Clamp(page, 1, 10_000);
        int normalizedLimit = Math.Clamp(limit, 1, 100);
        IReadOnlyList<MailRelayDeliveryEventEntry> all = await GetDeliveryEventsAsync(email, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MailRelayDeliveryEventEntry> data = all.Skip((normalizedPage - 1) * normalizedLimit).Take(normalizedLimit).ToArray();
        int totalPages = all.Count == 0 ? 0 : (int)Math.Ceiling(all.Count / (double)normalizedLimit);
        return new MailRelayPage<MailRelayDeliveryEventEntry>(data, normalizedPage, normalizedLimit, totalPages, all.Count);
    }
    Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetSuppressedRecipientsAsync(
        IReadOnlyCollection<string> recipients,
        CancellationToken cancellationToken);
    Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken);
    Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<DateTimeOffset?> MarkFailedAttemptAsync(QueuedEmailFailureDecision decision, CancellationToken cancellationToken);
}
