namespace FoodDiary.MailRelay.Application.DeliveryEvents.Services;

public sealed class MailRelayDeliveryEventIngestionService(IMailRelayQueueStore queueStore) {
    public async Task<Result<MailRelayDeliveryEventEntry>> IngestAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken) {
        if (!MailRelayDeliveryEventType.TryNormalize(request.EventType, out string? eventType)) {
            return Result<MailRelayDeliveryEventEntry>.Failure(MailRelayErrors.InvalidDeliveryEventType());
        }

        IngestMailEventRequest normalizedRequest = request with { EventType = eventType };
        MailRelayDeliveryEventEntry deliveryEvent = await queueStore.RecordDeliveryEventAsync(normalizedRequest, cancellationToken).ConfigureAwait(false);

        if (MailRelaySuppressionPolicy.ShouldSuppress(eventType, request.Classification)) {
            await queueStore.UpsertSuppressionAsync(
                new CreateSuppressionRequest(
                    request.Email,
                    request.Reason ?? MailRelaySuppressionPolicy.GetDefaultReason(eventType),
                    request.Source),
                cancellationToken).ConfigureAwait(false);
        }

        return Result<MailRelayDeliveryEventEntry>.Success(deliveryEvent);
    }

    public async Task<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> IngestManyAsync(
        IEnumerable<IngestMailEventRequest> requests,
        CancellationToken cancellationToken) {
        var result = new List<MailRelayDeliveryEventEntry>();
        foreach (IngestMailEventRequest request in requests) {
            Result<MailRelayDeliveryEventEntry> deliveryEvent = await IngestAsync(request, cancellationToken).ConfigureAwait(false);
            if (deliveryEvent.IsFailure) {
                return Result<IReadOnlyList<MailRelayDeliveryEventEntry>>.Failure(deliveryEvent.Error!);
            }

            result.Add(deliveryEvent.Value);
        }

        return Result<IReadOnlyList<MailRelayDeliveryEventEntry>>.Success(result);
    }
}
