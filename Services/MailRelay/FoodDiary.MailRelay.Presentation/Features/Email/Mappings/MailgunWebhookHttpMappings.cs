using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using System.Diagnostics;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Mappings;

public static class MailgunWebhookHttpMappings {
    public static bool TryMapToDeliveryEvent(
        this MailgunWebhookHttpRequest request,
        out IngestMailEventRequest? deliveryEvent,
        out string? error) {
        deliveryEvent = null;
        error = null;

        string eventType = request.EventData.Event.Trim().ToLowerInvariant();
        if (eventType is not ("complained" or "failed" or "bounced")) {
            error = $"Unsupported Mailgun event '{request.EventData.Event}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.EventData.Id)) {
            error = "Mailgun event-data.id is required for replay protection.";
            return false;
        }

        string? providerMessageId = request.EventData.Message?.Headers?.MessageId;
        deliveryEvent = eventType switch {
            "complained" => new IngestMailEventRequest(
                "complaint",
                request.EventData.Recipient,
                "mailgun-webhook",
                Classification: null,
                providerMessageId,
                request.EventData.Reason ?? "complaint",
                ProviderEventId: request.EventData.Id),
            "failed" or "bounced" => new IngestMailEventRequest(
                "bounce",
                request.EventData.Recipient,
                "mailgun-webhook",
                string.Equals(request.EventData.Severity, "permanent", StringComparison.OrdinalIgnoreCase) ? "hard" : "soft",
                providerMessageId,
                request.EventData.Reason,
                ProviderEventId: request.EventData.Id),
            _ => throw new UnreachableException(),
        };
        return true;
    }
}
