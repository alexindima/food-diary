namespace FoodDiary.MailRelay.Services;

public static class MailgunEventMapper {
    public static bool TryMap(
        MailgunWebhookRequest request,
        out IngestMailEventRequest? deliveryEvent,
        out string? error) {
        deliveryEvent = null;
        error = null;

        if (request.EventData is null) {
            error = "Mailgun event-data is required.";
            return false;
        }

        var eventType = request.EventData.Event.Trim().ToLowerInvariant();
        deliveryEvent = eventType switch {
            "complained" => new IngestMailEventRequest(
                "complaint",
                request.EventData.Recipient,
                "mailgun-webhook",
                null,
                request.EventData.Id,
                request.EventData.Reason ?? "complaint"),
            "failed" or "bounced" => new IngestMailEventRequest(
                "bounce",
                request.EventData.Recipient,
                "mailgun-webhook",
                string.Equals(request.EventData.Severity, "permanent", StringComparison.OrdinalIgnoreCase) ? "hard" : "soft",
                request.EventData.Id,
                request.EventData.Reason),
            _ => null
        };

        if (deliveryEvent is not null) {
            return true;
        }

        error = $"Unsupported Mailgun event '{request.EventData.Event}'.";
        return false;
    }
}
