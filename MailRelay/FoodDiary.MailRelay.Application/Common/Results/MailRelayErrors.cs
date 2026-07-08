namespace FoodDiary.MailRelay.Application.Common.Results;

public static class MailRelayErrors {
    public static Error InvalidDeliveryEventType() =>
        new(
            "MailRelay.DeliveryEvent.InvalidEventType",
            "EventType must be either 'bounce' or 'complaint'.",
            ErrorKind.Validation);

    public static Error MessageNotFound(Guid id) =>
        new(
            "MailRelay.Message.NotFound",
            $"Mail relay message '{id}' was not found.",
            ErrorKind.NotFound);

    public static Error SuppressionNotFound(string email) =>
        new(
            "MailRelay.Suppression.NotFound",
            $"Mail relay suppression for '{email}' was not found.",
            ErrorKind.NotFound);

    public static Error DirectMxRequiresSingleRecipientDomain() =>
        new(
            "MailRelay.Delivery.DirectMxMultipleRecipientDomains",
            "Direct MX delivery supports recipients from one domain per queued message.",
            ErrorKind.Validation);
}
