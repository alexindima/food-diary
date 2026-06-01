using System.Text.Json;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Mappings;

public static class AwsSesSnsWebhookHttpMappings {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryMapToDeliveryEvents(
        this AwsSesSnsWebhookHttpRequest request,
        out IReadOnlyList<IngestMailEventRequest> events,
        out string? error) {
        events = [];
        error = null;

        if (!string.Equals(request.Type, "Notification", StringComparison.OrdinalIgnoreCase)) {
            error = "Only SNS notifications with Type=Notification are supported.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Message)) {
            error = "SNS notification Message is required.";
            return false;
        }

        AwsSesNotificationHttpModel? notification;
        try {
            notification = JsonSerializer.Deserialize<AwsSesNotificationHttpModel>(request.Message, JsonOptions);
        } catch (JsonException ex) {
            error = $"Invalid SNS notification payload: {ex.Message}";
            return false;
        }

        if (notification is null) {
            error = "SNS notification payload could not be deserialized.";
            return false;
        }

        if (string.Equals(notification.NotificationType, "Bounce", StringComparison.OrdinalIgnoreCase)) {
            return TryMapBounce(notification, out events, out error);
        }

        if (string.Equals(notification.NotificationType, "Complaint", StringComparison.OrdinalIgnoreCase)) {
            return TryMapComplaint(notification, out events, out error);
        }

        error = $"Unsupported SES notification type '{notification.NotificationType}'.";
        return false;
    }

    private static bool TryMapBounce(
        AwsSesNotificationHttpModel notification,
        out IReadOnlyList<IngestMailEventRequest> events,
        out string? error) {
        var classification = string.Equals(notification.Bounce?.BounceType, "Permanent", StringComparison.OrdinalIgnoreCase)
            ? "hard"
            : "soft";

        events = notification.Bounce?.BouncedRecipients
                     .Where(static recipient => !string.IsNullOrWhiteSpace(recipient.EmailAddress))
                     .Select(recipient => new IngestMailEventRequest(
                         "bounce",
                         recipient.EmailAddress,
                         "aws-ses-sns",
                         classification,
                         notification.Mail.MessageId,
                         recipient.DiagnosticCode))
                     .ToArray()
                 ?? [];

        error = events.Count == 0
            ? "Bounce notification does not contain recipients."
            : null;
        return error is null;
    }

    private static bool TryMapComplaint(
        AwsSesNotificationHttpModel notification,
        out IReadOnlyList<IngestMailEventRequest> events,
        out string? error) {
        events = notification.Complaint?.ComplainedRecipients
                     .Where(static recipient => !string.IsNullOrWhiteSpace(recipient.EmailAddress))
                     .Select(recipient => new IngestMailEventRequest(
                         "complaint",
                         recipient.EmailAddress,
                         "aws-ses-sns",
                         null,
                         notification.Mail.MessageId,
                         "complaint"))
                     .ToArray()
                 ?? [];

        error = events.Count == 0
            ? "Complaint notification does not contain recipients."
            : null;
        return error is null;
    }
}
