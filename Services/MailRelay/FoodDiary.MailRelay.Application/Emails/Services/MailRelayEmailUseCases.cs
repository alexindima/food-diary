namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayEmailUseCases(
    IMailRelayQueueStore queueStore,
    IMailRelayDispatchNotifier dispatchNotifier,
    IMailRelayDeliveryPolicy deliveryPolicy) {
    public async Task<Result<Guid>> EnqueueAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        if (!RelayEmailMessageLimits.IsWithinBounds(request)) {
            return Result.Failure<Guid>(new Error("Validation.Invalid", "Email exceeds the recipient or message size limits.", Kind: ErrorKind.Validation));
        }

        Result policyResult = deliveryPolicy.CanEnqueue(request);
        if (policyResult.IsFailure) {
            return Result.Failure<Guid>(policyResult.Error!);
        }

        QueuedEmailId queuedEmailId = await queueStore.EnqueueAsync(request, cancellationToken).ConfigureAwait(false);
        await dispatchNotifier.NotifyQueuedAsync(queuedEmailId, cancellationToken).ConfigureAwait(false);
        return Result.Success(queuedEmailId.Value);
    }

    public Task<MailRelayQueueStats> GetStatsAsync(CancellationToken cancellationToken) {
        return queueStore.GetStatsAsync(cancellationToken);
    }

    public Task<MailRelayMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        return queueStore.GetMessageDetailsAsync(new QueuedEmailId(id), cancellationToken);
    }

    public Task<MailRelayPage<MailRelaySuppressionEntry>> GetSuppressionsPageAsync(
        string? email, int page, int limit, CancellationToken cancellationToken) {
        return queueStore.GetSuppressionsPageAsync(email, page, limit, cancellationToken);
    }

    public Task CreateSuppressionAsync(CreateSuppressionRequest request, CancellationToken cancellationToken) {
        return queueStore.UpsertSuppressionAsync(request, cancellationToken);
    }

    public Task<bool> RemoveSuppressionAsync(string email, CancellationToken cancellationToken) {
        return queueStore.RemoveSuppressionAsync(email, cancellationToken);
    }

    public Task<MailRelayPage<MailRelayDeliveryEventEntry>> GetDeliveryEventsPageAsync(
        string? email, int page, int limit, CancellationToken cancellationToken) {
        return queueStore.GetDeliveryEventsPageAsync(email, page, limit, cancellationToken);
    }
}
