namespace FoodDiary.MailRelay.Services;

public sealed class MailRelayDeliveryEventIngestionService(MailRelayQueueStore queueStore) {
    public async Task<MailRelayDeliveryEventEntry> IngestAsync(
        IngestMailEventRequest request,
        CancellationToken cancellationToken) {
        var eventType = request.EventType.Trim().ToLowerInvariant();
        if (eventType is not ("bounce" or "complaint")) {
            throw new InvalidOperationException("EventType must be either 'bounce' or 'complaint'.");
        }

        var normalizedRequest = request with { EventType = eventType };
        var deliveryEvent = await queueStore.RecordDeliveryEventAsync(normalizedRequest, cancellationToken);

        var shouldSuppress = eventType == "complaint" ||
                             (eventType == "bounce" &&
                              string.Equals(request.Classification, "hard", StringComparison.OrdinalIgnoreCase));

        if (shouldSuppress) {
            await queueStore.UpsertSuppressionAsync(
                new CreateSuppressionRequest(
                    request.Email,
                    request.Reason ?? (eventType == "complaint" ? "complaint" : "hard-bounce"),
                    request.Source),
                cancellationToken);
        }

        return deliveryEvent;
    }

    public async Task<IReadOnlyList<MailRelayDeliveryEventEntry>> IngestManyAsync(
        IEnumerable<IngestMailEventRequest> requests,
        CancellationToken cancellationToken) {
        var result = new List<MailRelayDeliveryEventEntry>();
        foreach (var request in requests) {
            result.Add(await IngestAsync(request, cancellationToken));
        }

        return result;
    }
}
