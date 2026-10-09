namespace FoodDiary.MailRelay.Domain.Emails;

public enum QueuedEmailProcessingState {
    Unrecognized = 0,
    Pending = 1,
    Retry = 2,
    Processing = 3,
    Sent = 4,
    Failed = 5,
    Suppressed = 6,
}
