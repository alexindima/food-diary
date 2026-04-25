namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayEmailUseCases(
    IMailRelayQueueStore queueStore,
    IMailRelayDispatchNotifier dispatchNotifier) {
    public async Task<Guid> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        var queuedEmailId = await queueStore.EnqueueAsync(request, cancellationToken);
        await dispatchNotifier.NotifyQueuedAsync(queuedEmailId, cancellationToken);
        return queuedEmailId;
    }

    public Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) {
        return queueStore.GetStatsAsync(cancellationToken);
    }

    public Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        return queueStore.GetMessageDetailsAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<MailRelaySuppressionEntry>> GetSuppressionsAsync(
        string? email,
        CancellationToken cancellationToken) {
        return queueStore.GetSuppressionsAsync(email, cancellationToken);
    }

    public Task CreateSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) {
        return queueStore.UpsertSuppressionAsync(request, cancellationToken);
    }

    public Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) {
        return queueStore.RemoveSuppressionAsync(email, cancellationToken);
    }

    public Task<IReadOnlyList<MailRelayDeliveryEventEntry>> GetDeliveryEventsAsync(
        string? email,
        CancellationToken cancellationToken) {
        return queueStore.GetDeliveryEventsAsync(email, cancellationToken);
    }
}
