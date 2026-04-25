namespace FoodDiary.MailRelay.Application.Common.Result;

public static class MailRelayErrors {
    public static MailRelayError InvalidDeliveryEventType() =>
        new(
            "MailRelay.DeliveryEvent.InvalidEventType",
            "EventType must be either 'bounce' or 'complaint'.",
            ErrorKind.Validation);

    public static MailRelayError MessageNotFound(Guid id) =>
        new(
            "MailRelay.Message.NotFound",
            $"Mail relay message '{id}' was not found.",
            ErrorKind.NotFound);

    public static MailRelayError SuppressionNotFound(string email) =>
        new(
            "MailRelay.Suppression.NotFound",
            $"Mail relay suppression for '{email}' was not found.",
            ErrorKind.NotFound);
}
