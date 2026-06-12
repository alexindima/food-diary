namespace FoodDiary.MailRelay.Domain.Emails;

public static class QueuedEmailStatus {
    public const string Pending = "pending";
    public const string Retry = "retry";
    public const string Processing = "processing";
    public const string Sent = "sent";
    public const string Failed = "failed";
    public const string Suppressed = "suppressed";
}
