namespace FoodDiary.MailRelay.Application.DeliveryEvents.Services;

public sealed class MailRelayDeliveryEventIngestionService(IMailRelayQueueStore queueStore) {
    public async Task<Result<MailRelayDeliveryEventEntry>> IngestAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken) {
        if (!MailRelayDeliveryEventType.TryNormalize(request.EventType, out var eventType)) {
            return Result<MailRelayDeliveryEventEntry>.Failure(MailRelayErrors.InvalidDeliveryEventType());
        }

        var normalizedRequest = request with { EventType = eventType };
        var deliveryEvent = await queueStore.RecordDeliveryEventAsync(normalizedRequest, cancellationToken);

        if (MailRelaySuppressionPolicy.ShouldSuppress(eventType, request.Classification)) {
            await queueStore.UpsertSuppressionAsync(
                new CreateSuppressionRequest(
                    request.Email,
                    request.Reason ?? MailRelaySuppressionPolicy.GetDefaultReason(eventType),
                    request.Source),
                cancellationToken);
        }

        return Result<MailRelayDeliveryEventEntry>.Success(deliveryEvent);
    }

    public async Task<Result<IReadOnlyList<MailRelayDeliveryEventEntry>>> IngestManyAsync(
        IEnumerable<IngestMailEventRequest> requests,
        CancellationToken cancellationToken) {
        var result = new List<MailRelayDeliveryEventEntry>();
        foreach (var request in requests) {
            var deliveryEvent = await IngestAsync(request, cancellationToken);
            if (deliveryEvent.IsFailure) {
                return Result<IReadOnlyList<MailRelayDeliveryEventEntry>>.Failure(deliveryEvent.Error!);
            }

            result.Add(deliveryEvent.Value);
        }

        return Result<IReadOnlyList<MailRelayDeliveryEventEntry>>.Success(result);
    }
}
