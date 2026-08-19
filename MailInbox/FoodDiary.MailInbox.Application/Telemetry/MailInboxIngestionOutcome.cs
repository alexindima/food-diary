namespace FoodDiary.MailInbox.Application.Telemetry;

public enum MailInboxIngestionOutcome {
    Overloaded = 0,
    EmptyMessage = 1,
    MessageTooLarge = 2,
    IpByteRateLimited = 3,
    MimePartLimit = 4,
    RecipientLimit = 5,
    MetadataLimit = 6,
    Duplicate = 7,
    Success = 8,
    Canceled = 9,
    StorageQuota = 10,
    Failure = 11,
}
