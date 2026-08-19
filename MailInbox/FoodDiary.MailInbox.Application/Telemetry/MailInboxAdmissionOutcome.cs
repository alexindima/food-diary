namespace FoodDiary.MailInbox.Application.Telemetry;

public enum MailInboxAdmissionOutcome {
    MessageTooLarge = 0,
    SessionRateLimited = 1,
    IpRateLimited = 2,
    SenderRateLimited = 3,
    Accepted = 4,
    RecipientNotAllowed = 5,
    RecipientLimitExceeded = 6,
}
