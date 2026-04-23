using System.Text.Json;

namespace FoodDiary.MailRelay.Services;

public static class AwsSesSnsEventMapper {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryMap(
        AwsSesSnsWebhookRequest request,
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

        AwsSesNotificationMessage? notification;
        try {
            notification = JsonSerializer.Deserialize<AwsSesNotificationMessage>(request.Message, JsonOptions);
        } catch (JsonException ex) {
            error = $"Invalid SNS notification payload: {ex.Message}";
            return false;
        }

        if (notification is null) {
            error = "SNS notification payload could not be deserialized.";
            return false;
        }

        var source = "aws-ses-sns";
        if (string.Equals(notification.NotificationType, "Bounce", StringComparison.OrdinalIgnoreCase)) {
            var classification = string.Equals(notification.Bounce?.BounceType, "Permanent", StringComparison.OrdinalIgnoreCase)
                ? "hard"
                : "soft";

            events = notification.Bounce?.BouncedRecipients
                         .Where(static recipient => !string.IsNullOrWhiteSpace(recipient.EmailAddress))
                         .Select(recipient => new IngestMailEventRequest(
                             "bounce",
                             recipient.EmailAddress,
                             source,
                             classification,
                             notification.Mail.MessageId,
                             recipient.DiagnosticCode))
                         .ToArray()
                     ?? [];

            if (events.Count == 0) {
                error = "Bounce notification does not contain recipients.";
                return false;
            }

            return true;
        }

        if (string.Equals(notification.NotificationType, "Complaint", StringComparison.OrdinalIgnoreCase)) {
            events = notification.Complaint?.ComplainedRecipients
                         .Where(static recipient => !string.IsNullOrWhiteSpace(recipient.EmailAddress))
                         .Select(recipient => new IngestMailEventRequest(
                             "complaint",
                             recipient.EmailAddress,
                             source,
                             null,
                             notification.Mail.MessageId,
                             "complaint"))
                         .ToArray()
                     ?? [];

            if (events.Count == 0) {
                error = "Complaint notification does not contain recipients.";
                return false;
            }

            return true;
        }

        error = $"Unsupported SES notification type '{notification.NotificationType}'.";
        return false;
    }
}
